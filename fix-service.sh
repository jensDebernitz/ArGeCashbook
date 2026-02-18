#!/bin/bash

echo "🔧 ArGe Kassenbuch Service Fix Script"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   print_error "This script must be run as root (use sudo)"
   exit 1
fi

print_status "Checking .NET Runtime..."
if ! dotnet --list-runtimes | grep -q "aspnetcore-runtime-8.0"; then
    print_warning ".NET 8.0 Runtime not found. Installing..."
    apt-get update
    apt-get install -y aspnetcore-runtime-8.0
else
    print_status ".NET 8.0 Runtime found"
fi

print_status "Checking application directory..."
if [ ! -d "/opt/argekassenbuch/published" ]; then
    print_error "Application directory not found!"
    exit 1
fi

print_status "Checking DLL file..."
if [ ! -f "/opt/argekassenbuch/published/ArgeKassenbuch.dll" ]; then
    print_error "ArgeKassenbuch.dll not found!"
    exit 1
fi

print_status "Fixing permissions..."
chown -R www-data:www-data /opt/argekassenbuch/published
chmod -R 755 /opt/argekassenbuch/published

print_status "Testing PostgreSQL connection..."
if ! sudo -u postgres psql -U kassenbuch -d arge_kassenbuch -c "SELECT 1;" &>/dev/null; then
    print_warning "PostgreSQL connection failed. Recreating database..."
    sudo -u postgres psql -c "DROP DATABASE IF EXISTS arge_kassenbuch;" || true
    sudo -u postgres psql -c "CREATE DATABASE arge_kassenbuch OWNER kassenbuch;" || true
    sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE arge_kassenbuch TO kassenbuch;" || true
fi

print_status "Checking port 8000..."
if netstat -tlnp | grep -q ":8000 "; then
    print_warning "Port 8000 is already in use. Killing process..."
    sudo fuser -k 8000/tcp || true
    sleep 2
fi

print_status "Recreating systemd service..."
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

print_status "Reloading systemd daemon..."
systemctl daemon-reload

print_status "Starting service..."
systemctl start argekassenbuch
systemctl enable argekassenbuch

sleep 3

print_status "Checking service status..."
if systemctl is-active --quiet argekassenbuch; then
    print_status "✅ Service is running!"
    echo
    echo "Service details:"
    systemctl status argekassenbuch --no-pager -l
    echo
    echo "Recent logs:"
    journalctl -u argekassenbuch --no-pager -n 20
else
    print_error "❌ Service failed to start!"
    echo
    echo "Service status:"
    systemctl status argekassenbuch --no-pager -l
    echo
    echo "Error logs:"
    journalctl -u argekassenbuch --no-pager -n 30
    echo
    echo "Manual test:"
    echo "Run: sudo -u www-data dotnet /opt/argekassenbuch/published/ArgeKassenbuch.dll"
fi
