#!/bin/bash

# ArGe Kassenbuch Ubuntu Deployment Script
# Usage: sudo ./deploy-ubuntu.sh

set -e

echo "🚀 ArGe Kassenbuch Ubuntu Deployment"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   print_error "This script must be run as root (use sudo)"
   exit 1
fi

# Update system
print_status "Updating system packages..."
apt update && apt upgrade -y

# Install .NET 8.0 Runtime
print_status "Installing .NET 8.0 Runtime..."
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
apt-get update && apt-get install -y aspnetcore-runtime-8.0

# Install PostgreSQL
print_status "Installing PostgreSQL..."
apt install postgresql postgresql-contrib -y
systemctl start postgresql
systemctl enable postgresql

# Create database and user
print_status "Setting up PostgreSQL database..."
sudo -u postgres psql -c "CREATE USER kassenbuch WITH PASSWORD 'kassenbuch123';" || true
sudo -u postgres psql -c "CREATE DATABASE arge_kassenbuch OWNER kassenbuch;" || true
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE arge_kassenbuch TO kassenbuch;" || true

# Create application directory
print_status "Creating application directory..."
mkdir -p /opt/argekassenbuch
cd /opt/argekassenbuch

# Get current directory (where script is located)
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Copy application files
print_status "Copying application files..."
cp -r "$SCRIPT_DIR"/* /opt/argekassenbuch/
chown -R www-data:www-data /opt/argekassenbuch
chmod -R 755 /opt/argekassenbuch

# Install .NET SDK temporarily for building
print_status "Installing .NET SDK for build..."
apt-get install -y dotnet-sdk-8.0

# Build application
print_status "Building application..."
cd /opt/argekassenbuch
sudo -u www-data dotnet publish ArgeKassenbuch.csproj -c Release -o /opt/argekassenbuch/published --no-self-contained

# Remove SDK
print_status "Removing .NET SDK..."
apt-get remove -y dotnet-sdk-8.0

# Create systemd service
print_status "Creating systemd service..."
cat > /etc/systemd/system/argekassenbuch.service << 'EOF'
[Unit]
Description=ArGe Kassenbuch
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/argekassenbuch/published
ExecStart=/usr/bin/dotnet ArgeKassenbuch.dll
Environment=ASPNETCORE_ENVIRONMENT=Production
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=argekassenbuch
User=www-data
Group=www-data

[Install]
WantedBy=multi-user.target
EOF

# Start service
print_status "Starting application service..."
systemctl daemon-reload
systemctl start argekassenbuch
systemctl enable argekassenbuch

# Install Nginx (optional)
read -p "Do you want to install Nginx as reverse proxy? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    print_status "Installing Nginx..."
    apt install nginx -y
    systemctl start nginx
    systemctl enable nginx
    
    # Get server IP or domain
    read -p "Enter your domain name or server IP: " SERVER_NAME
    
    # Create Nginx config
    cat > /etc/nginx/sites-available/argekassenbuch << EOF
server {
    listen 80;
    server_name $SERVER_NAME;

    location / {
        proxy_pass http://127.0.0.1:8000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 86400;
    }
}
EOF
    
    # Enable site
    ln -s /etc/nginx/sites-available/argekassenbuch /etc/nginx/sites-enabled/
    rm -f /etc/nginx/sites-enabled/default
    nginx -t
    systemctl restart nginx
    
    print_status "Nginx configured for $SERVER_NAME"
fi

# Setup firewall
print_status "Configuring firewall..."
ufw --force enable
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp

# Create backup script
print_status "Setting up backup script..."
mkdir -p /opt/backups/kassenbuch
cat > /usr/local/bin/backup-kassenbuch.sh << 'EOF'
#!/bin/bash
BACKUP_DIR="/opt/backups/kassenbuch"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Database backup
pg_dump -h localhost -U kassenbuch arge_kassenbuch > $BACKUP_DIR/db_$DATE.sql

# App files backup
tar -czf $BACKUP_DIR/app_$DATE.tar.gz /opt/argekassenbuch/published

# Clean old backups
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete

echo "Backup completed: $DATE"
EOF

chmod +x /usr/local/bin/backup-kassenbuch.sh

# Add backup cron job
(crontab -l 2>/dev/null; echo "0 2 * * * /usr/local/bin/backup-kassenbuch.sh") | crontab -

# Show status
print_status "Deployment completed! 🎉"
echo
echo "Service status:"
systemctl status argekassenbuch --no-pager
echo
echo "Application logs:"
journalctl -u argekassenbuch --no-pager -n 20
echo
echo "Access your application at:"
if systemctl is-active --quiet nginx; then
    echo "http://your-domain-or-ip"
else
    echo "http://your-server-ip:8000"
fi
echo
echo "Default login: admin"
echo "Don't forget to change the admin password!"
echo
echo "Backup script: /usr/local/bin/backup-kassenbuch.sh"
echo "Backup location: /opt/backups/kassenbuch"
