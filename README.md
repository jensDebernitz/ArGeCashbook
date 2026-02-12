# ArGe Kassenbuch

Digitales Kassenbuch für die Arbeitsgemeinschaft Oberfell (ArGe Oberfell), bestehend aus vier Vereinen:

- **Feuerwehrkameradschaft (FK)**
- **Sportverein (SV)**
- **Karnevalsverein (KV)**
- **Heimat- und Verkehrsverein (HVV)**

## Veranstaltungen

| Veranstaltung | Beteiligte Vereine |
|---|---|
| Heimatfeste (2x jährlich) | FK, SV, KV, HVV |
| Karneval (Kappensitzung, Kinderkarneval, Umzug) | FK, SV, KV |
| Weinkirmes (jährlich) | FK, SV, HVV |

## Funktionen

### Verkauf & Bons
- Digitale Bon-Erstellung mit Artikelauswahl pro Verkaufsstand
- Bedienungs-Modus (gesperrter Bildschirm, nur per Admin-Passwort verlassbar)
- Storno-Funktion
- Vorbereitung für ESC/POS Bondrucker-Integration

### Kassenverwaltung
- Wechselgeld-Ausgabe und -Rücknahme tracken
- SafeBag-Management (erstellen, im Tresor verwalten, entnehmen)
- Kassenbuch-Übersicht mit Umsatzstatistiken und Top-Artikeln

### Schichtplan & Übergabe
- Schichtplanung mit Name, Telefon, Zeitraum und Rolle
- Kassenübergabe-Protokoll (SafeBag-Anzahl, Wechselgeldhöhe, Schlüsselübergabe)

### Stammdaten
- Vereine verwalten
- Verkaufsstände anlegen (z.B. Biertheke, Essensstand)
- Waren & Preise pro Stand konfigurieren
- Bedienungen registrieren
- Veranstaltungen mit teilnehmenden Vereinen erstellen

## Technologie

- **Framework:** ASP.NET 8.0 / Blazor Server (Interactive Server-Side Rendering)
- **Datenbank:** PostgreSQL mit Entity Framework Core
- **UI:** Bootstrap 5 + Bootstrap Icons
- **ORM:** Npgsql.EntityFrameworkCore.PostgreSQL 8.0.x

## Voraussetzungen

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (lokal oder im Netzwerk)

## Installation & Start

### 1. PostgreSQL bereitstellen

PostgreSQL muss auf `localhost:5432` laufen. Die Datenbank `arge_kassenbuch` wird beim ersten Start automatisch erstellt.

Standard-Zugangsdaten (konfigurierbar in `appsettings.json`):
- **User:** `postgres`
- **Password:** `postgres`

### 2. Anwendung starten

```bash
dotnet run --urls "http://localhost:5050"
```

Die Anwendung ist dann unter `http://localhost:5050` erreichbar.

### 3. Erste Schritte

1. Die vier Vereine werden automatisch beim ersten Start angelegt
2. Unter **Verkaufsstände** die Stände anlegen (z.B. "Biertheke", "Essensstand")
3. Unter **Waren & Preise** die Artikel mit Preisen pro Stand konfigurieren
4. Unter **Bedienungen** die externen Bedienungen registrieren
5. Unter **Veranstaltungen** eine neue Veranstaltung anlegen und aktivieren
6. Unter **Schichtplan** die Schichten planen
7. Verkauf starten über **Verkauf / Bons** oder **Bedienungs-Modus**

## Konfiguration

Die PostgreSQL-Verbindung wird in `appsettings.json` konfiguriert:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=arge_kassenbuch;Username=postgres;Password=postgres"
  }
}
```

## Bedienungs-Modus

Der Bedienungs-Modus ist ein gesperrter Bereich für die externen Bedienungen:

1. Bedienung auswählen
2. Verkaufsstand auswählen
3. Artikel per Touch/Klick erfassen
4. Bon abschließen
5. **Zum Verlassen ist das Admin-Passwort erforderlich** (Standard: `admin`)

Das Admin-Passwort kann unter **Einstellungen** geändert werden.

## Geplante Erweiterungen

- [ ] ESC/POS Bondrucker-Integration (Epson TM-T20III o.ä.)
- [ ] Bon-Druck mit Durchschlag-Simulation
- [ ] Export/Reporting (PDF, Excel)
- [ ] Mehrere Kassen gleichzeitig
- [ ] Offline-Modus
