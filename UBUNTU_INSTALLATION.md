# ArGe Kassenbuch - Ubuntu Installation ohne Docker

## Voraussetzungen
- Ubuntu 20.04 oder neuer
- PostgreSQL 12 oder neuer
- .NET 8.0 Runtime

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

### Verbindung testen
```bash
psql -h localhost -U kassenbuch -d arge_kassenbuch -W
# Passwort: kassenbuch123
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
# Von deinem lokalen Rechner kopieren (ersetze mit deinem Pfad)
# scp -r /pfad/zu/ArgeKassenbuch/* user@server:/opt/argekassenbuch/

# Oder direkt auf dem Server klonen (wenn Git verfügbar)
git clone https://github.com/jensDebernitz/ArGeCashbook.git .
```

### Anwendung veröffentlichen
```bash
# .NET 8.0 SDK wird zum Build benötigt (temporär installieren)
sudo apt-get install -y dotnet-sdk-8.0

# Anwendung für Production bauen
dotnet publish ArgeKassenbuch.csproj -c Release -o /opt/argekassenbuch/published --no-self-contained

# SDK wieder entfernen (optional)
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
# Service neu laden und starten
sudo systemctl daemon-reload
sudo systemctl start argekassenbuch
sudo systemctl enable argekassenbuch

# Status prüfen
sudo systemctl status argekassenbuch
```

## 6. Nginx als Reverse Proxy (optional aber empfohlen)

### Nginx installieren
```bash
sudo apt install nginx -y
sudo systemctl start nginx
sudo systemctl enable nginx
```

### Nginx Konfiguration erstellen
```bash
sudo nano /etc/nginx/sites-available/argekassenbuch
```

Inhalt:
```nginx
server {
    listen 80;
    server_name deine-domain.de;  # Ersetzen mit deiner Domain/IP

    location / {
        proxy_pass http://127.0.0.1:8000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 86400;
    }
}
```

### Site aktivieren
```bash
sudo ln -s /etc/nginx/sites-available/argekassenbuch /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

## 7. Firewall konfigurieren

### UFW Firewall einrichten
```bash
# Firewall aktivieren
sudo ufw enable

# Ports erlauben
sudo ufw allow ssh
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Status prüfen
sudo ufw status
```

## 8. SSL mit Let's Encrypt (optional)

### Certbot installieren
```bash
sudo apt install certbot python3-certbot-nginx -y
```

### Zertifikat erstellen
```bash
sudo certbot --nginx -d deine-domain.de
```

### Auto-Renewal einrichten
```bash
sudo crontab -e
# Zeile hinzufügen:
0 12 * * * /usr/bin/certbot renew --quiet
```

## 9. Backup-Script erstellen

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

## 10. Überwachung

### Logs ansehen
```bash
# Application Logs
sudo journalctl -u argekassenbuch -f

# Nginx Logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### Service Status
```bash
sudo systemctl status argekassenbuch
sudo systemctl status nginx
sudo systemctl status postgresql
```

## 11. First Setup

Nach dem ersten Start:
1. Browser öffnen: `http://deine-domain.de` oder `http://server-ip:8000`
2. Login mit Passwort: `admin`
3. Admin-Passwort unter Einstellungen ändern
4. Erste Veranstaltung anlegen

## Fertig! 🎉

Die ArGe Kassenbuch läuft jetzt auf Port 8000 (oder über Nginx auf Port 80/443).

### Wichtige Pfade:
- Anwendung: `/opt/argekassenbuch/published`
- Logs: `journalctl -u argekassenbuch`
- Config: `/opt/argekassenbuch/published/appsettings.Production.json`
- Backups: `/opt/backups/kassenbuch`
- Nginx Config: `/etc/nginx/sites-available/argekassenbuch`
