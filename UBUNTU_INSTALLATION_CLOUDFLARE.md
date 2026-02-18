# ArGe Kassenbuch - Ubuntu Installation mit Cloudflare (ohne Nginx)

## Voraussetzungen
- Ubuntu 20.04 oder neuer
- PostgreSQL 12 oder neuer
- .NET 8.0 Runtime
- Cloudflare Account mit Domain

## 1. System vorbereiten

### Updates installieren
```bash
sudo apt update && sudo apt upgrade -y
```

### .NET 8.0 Runtime installieren
```bash
# Microsoft Repository hinzufügen
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# .NET 8.0 Runtime installieren
sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-8.0
```

### PostgreSQL installieren
```bash
# PostgreSQL installieren
sudo apt install postgresql postgresql-contrib -y

# PostgreSQL starten und aktivieren
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

## 2. Datenbank einrichten

### PostgreSQL Benutzer und Datenbank erstellen
```bash
# Als postgres Benutzer anmelden
sudo -u postgres psql

# In PostgreSQL Shell ausführen:
CREATE USER kassenbuch WITH PASSWORD 'kassenbuch123';
CREATE DATABASE arge_kassenbuch OWNER kassenbuch;
GRANT ALL PRIVILEGES ON DATABASE arge_kassenbuch TO kassenbuch;
\q
```

## 3. Anwendung部署

### Verzeichnis erstellen
```bash
sudo mkdir -p /opt/argekassenbuch
sudo chown $USER:$USER /opt/argekassenbuch
cd /opt/argekassenbuch
```

### Anwendung kopieren
```bash
# Von deinem lokalen Rechner kopieren
# scp -r /pfad/zu/ArgeKassenbuch/* user@server:/opt/argekassenbuch/

# Oder direkt auf dem Server klonen
git clone https://github.com/jensDebernitz/ArGeCashbook.git .
```

### Anwendung veröffentlichen
```bash
# .NET 8.0 SDK zum Build installieren
sudo apt-get install -y dotnet-sdk-8.0

# Anwendung für Production bauen
dotnet publish ArgeKassenbuch.csproj -c Release -o /opt/argekassenbuch/published --no-self-contained

# SDK wieder entfernen
sudo apt-get remove -y dotnet-sdk-8.0
```

## 4. Konfiguration

### Production Settings erstellen
```bash
cd /opt/argekassenbuch/published
nano appsettings.Production.json
```

Inhalt:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=arge_kassenbuch;Username=kassenbuch;Password=kassenbuch123;Port=5432"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Berechtigungen setzen
```bash
sudo chown -R www-data:www-data /opt/argekassenbuch/published
sudo chmod -R 755 /opt/argekassenbuch/published
```

## 5. Systemd Service erstellen

```bash
sudo nano /etc/systemd/system/argekassenbuch.service
```

Inhalt:
```ini
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
```

### Service starten
```bash
sudo systemctl daemon-reload
sudo systemctl start argekassenbuch
sudo systemctl enable argekassenbuch

# Status prüfen
sudo systemctl status argekassenbuch
```

## 6. Cloudflare Konfiguration

### Cloudflare DNS Settings
1. **Cloudflare Dashboard** → deine Domain
2. **DNS** → **Add Record**
   - **Type**: A
   - **Name**: kassenbuch (oder dein Wunschname)
   - **IPv4 address**: deine Server-IP
   - **Proxy status**: Proxied (orange Wolke)
   - **TTL**: Auto

### Cloudflare SSL/TLS Settings
1. **SSL/TLS** → **Overview**
   - **Flexible SSL** (wenn du kein SSL auf dem Server willst)
   - **Full SSL** (wenn du später eigenes SSL hinzufügst)

### Cloudflare Page Rules (optional für Performance)
1. **Rules** → **Page Rules**
2. **Create Page Rule**:
   - URL: `kassenbuch.deinedomain.de/*`
   - Settings:
     - **Cache Level**: Cache Everything
     - **Browser Cache TTL**: 4 hours
     - **Security Level**: Medium

## 7. Firewall konfigurieren

### UFW Firewall einrichten
```bash
# Firewall aktivieren
sudo ufw enable

# Ports erlauben (nur SSH und direkten Port 8000 als Fallback)
sudo ufw allow ssh
sudo ufw allow 8000/tcp

# Status prüfen
sudo ufw status
```

## 8. Backup-Script erstellen

```bash
sudo nano /usr/local/bin/backup-kassenbuch.sh
```

Inhalt:
```bash
#!/bin/bash
BACKUP_DIR="/opt/backups/kassenbuch"
DATE=$(date +%Y%m%d_%H%M%S)

# Backup Verzeichnis erstellen
mkdir -p $BACKUP_DIR

# Datenbank Backup
pg_dump -h localhost -U kassenbuch arge_kassenbuch > $BACKUP_DIR/db_$DATE.sql

# App Files Backup
tar -czf $BACKUP_DIR/app_$DATE.tar.gz /opt/argekassenbuch/published

# Alte Backups löschen (älter als 7 Tage)
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete

echo "Backup completed: $DATE"
```

```bash
# Ausführbar machen
sudo chmod +x /usr/local/bin/backup-kassenbuch.sh

# Cron Job für tägliches Backup um 2 Uhr nachts
sudo crontab -e
# Zeile hinzufügen:
0 2 * * * /usr/local/bin/backup-kassenbuch.sh
```

## 9. Automatisches Deployment Script (Cloudflare Version)
```bash
nano deploy-ubuntu-cloudflare.sh
```

Inhalt:
```bash
#!/bin/bash

# ArGe Kassenbuch Ubuntu Deployment Script mit Cloudflare
# Usage: sudo ./deploy-ubuntu-cloudflare.sh

set -e

echo "🚀 ArGe Kassenbuch Ubuntu Deployment mit Cloudflare"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

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

# Get current directory
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

# Setup firewall (Cloudflare needs only SSH and direct port as fallback)
print_status "Configuring firewall for Cloudflare..."
ufw --force enable
ufw allow ssh
ufw allow 8000/tcp

# Create backup script
print_status "Setting up backup script..."
mkdir -p /opt/backups/kassenbuch
cat > /usr/local/bin/backup-kassenbuch.sh << 'EOF'
#!/bin/bash
BACKUP_DIR="/opt/backups/kassenbuch"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR
pg_dump -h localhost -U kassenbuch arge_kassenbuch > $BACKUP_DIR/db_$DATE.sql
tar -czf $BACKUP_DIR/app_$DATE.tar.gz /opt/argekassenbuch/published
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
echo "Cloudflare Setup:"
echo "1. Add A record in Cloudflare DNS pointing to this server IP"
echo "2. Enable SSL (Flexible or Full)"
echo "3. Access via: https://kassenbuch.deinedomain.de"
echo "4. Fallback: http://$(curl -s ifconfig.me):8000"
echo
echo "Default login: admin"
echo "Don't forget to change the admin password!"
```

```bash
chmod +x deploy-ubuntu-cloudflare.sh
```

## 10. Überwachung

### Logs ansehen
```bash
# Application Logs
sudo journalctl -u argekassenbuch -f

# Service Status
sudo systemctl status argekassenbuch
sudo systemctl status postgresql
```

## 11. Cloudflare Vorteile

### Was Cloudflare für dich übernimmt:
- ✅ **SSL/TLS** (HTTPS ohne Zertifikat auf dem Server)
- ✅ **DDoS Protection**
- ✅ **CDN & Caching** (schnellerer Zugriff)
- ✅ **Load Balancing**
- ✅ **Security Features** (WAF, Rate Limiting)
- ✅ **Analytics**

### Performance Optimierung:
- **Static Files** werden von Cloudflare gecacht
- **API Calls** gehen direkt an deinen Server
- **Compression** und **Minification** automatisch

## 12. First Setup

Nach dem ersten Start:
1. **Cloudflare DNS** konfigurieren (A-Record)
2. **Browser öffnen**: `https://kassenbuch.deinedomain.de`
3. **Login** mit Passwort: `admin`
4. **Admin-Passwort** unter Einstellungen ändern
5. **Erste Veranstaltung** anlegen

## Fertig! 🎉

Die ArGe Kassenbuch läuft jetzt hinter Cloudflare auf Port 8000.

### Wichtige Pfade:
- Anwendung: `/opt/argekassenbuch/published`
- Logs: `journalctl -u argekassenbuch`
- Config: `/opt/argekassenbuch/published/appsettings.Production.json`
- Backups: `/opt/backups/kassenbuch`

### URLs:
- **Cloudflare**: `https://kassenbuch.deinedomain.de`
- **Fallback**: `http://server-ip:8000`
