#!/bin/bash
# =============================================================
# ArGe Kassenbuch - Debian Server Setup
# Erstellt den systemd-Service und installiert .NET Runtime
# =============================================================
set -e

echo "=== ArGe Kassenbuch - Debian Server Setup ==="

# .NET Runtime installieren (falls nicht vorhanden)
if ! command -v dotnet &> /dev/null; then
    echo ">> .NET Runtime wird installiert..."
    wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    sudo /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
    sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
    rm /tmp/dotnet-install.sh
    echo ">> .NET Runtime installiert: $(dotnet --version)"
else
    echo ">> .NET Runtime bereits vorhanden: $(dotnet --version)"
fi

# Benutzer anlegen
if ! id "kassenbuch" &>/dev/null; then
    echo ">> Benutzer 'kassenbuch' wird angelegt..."
    sudo useradd -r -s /bin/false -m -d /opt/kassenbuch kassenbuch
else
    echo ">> Benutzer 'kassenbuch' existiert bereits."
fi

# App-Verzeichnis
sudo mkdir -p /opt/kassenbuch
sudo chown -R kassenbuch:kassenbuch /opt/kassenbuch

# Production-Konfiguration anlegen (falls nicht vorhanden)
if [ ! -f /opt/kassenbuch/appsettings.Production.json ]; then
    echo ">> appsettings.Production.json wird erstellt..."
    cat <<'EOF' | sudo tee /opt/kassenbuch/appsettings.Production.json > /dev/null
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=arge_kassenbuch;Username=kassenbuch;Password=HIER_PASSWORT_AENDERN"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
EOF
    sudo chown kassenbuch:kassenbuch /opt/kassenbuch/appsettings.Production.json
    echo ""
    echo "!! WICHTIG: Passwort in /opt/kassenbuch/appsettings.Production.json anpassen !!"
    echo ""
fi

# systemd Service erstellen
echo ">> systemd Service wird erstellt..."
cat <<'EOF' | sudo tee /etc/systemd/system/kassenbuch.service > /dev/null
[Unit]
Description=ArGe Kassenbuch Web Application
After=network.target postgresql.service

[Service]
Type=notify
User=kassenbuch
Group=kassenbuch
WorkingDirectory=/opt/kassenbuch
ExecStart=/usr/bin/dotnet /opt/kassenbuch/ArgeKassenbuch.dll
Restart=always
RestartSec=10
SyslogIdentifier=kassenbuch
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:8000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

# Service aktivieren
sudo systemctl daemon-reload
sudo systemctl enable kassenbuch.service

echo ""
echo "=== Setup abgeschlossen ==="
echo ""
echo "Nächste Schritte:"
echo "  1. PostgreSQL installieren und DB/User anlegen:"
echo "     sudo -u postgres createuser kassenbuch"
echo "     sudo -u postgres createdb -O kassenbuch arge_kassenbuch"
echo "  2. Passwort in /opt/kassenbuch/appsettings.Production.json anpassen"
echo "  3. App-Dateien nach /opt/kassenbuch/ kopieren"
echo "  4. Service starten: sudo systemctl start kassenbuch"
echo "  5. Status prüfen: sudo systemctl status kassenbuch"
echo "  6. Logs ansehen: sudo journalctl -u kassenbuch -f"
echo ""
echo "GitHub Secrets für automatisches Deployment:"
echo "  DEPLOY_HOST     = IP/Hostname des Servers"
echo "  DEPLOY_USER     = SSH-Benutzer (mit sudo-Rechten)"
echo "  DEPLOY_SSH_KEY  = Privater SSH-Schlüssel"
echo "  DEPLOY_PORT     = SSH-Port (Standard: 22)"
