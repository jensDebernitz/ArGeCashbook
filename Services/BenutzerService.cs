using Microsoft.EntityFrameworkCore;
using ArgeKassenbuch.Data;
using ArgeKassenbuch.Models;

namespace ArgeKassenbuch.Services;

public class BenutzerService
{
    private readonly AppDbContext _db;

    public BenutzerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Benutzer?> LoginAsync(string benutzername, string passwort)
    {
        var benutzer = await _db.Benutzer
            .FirstOrDefaultAsync(b => b.Benutzername == benutzername && b.Aktiv);

        if (benutzer == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(passwort, benutzer.PasswortHash))
            return null;

        benutzer.LetzterLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return benutzer;
    }

    public async Task<List<Benutzer>> GetAlleBenutzerAsync()
        => await _db.Benutzer.OrderBy(b => b.Benutzername).ToListAsync();

    public async Task<Benutzer?> GetBenutzerAsync(int id)
        => await _db.Benutzer.FindAsync(id);

    public async Task<Benutzer?> GetBenutzerByNameAsync(string benutzername)
        => await _db.Benutzer.FirstOrDefaultAsync(b => b.Benutzername == benutzername);

    public async Task<bool> BenutzerExistiertAsync(string benutzername)
        => await _db.Benutzer.AnyAsync(b => b.Benutzername == benutzername);

    public async Task<Benutzer> ErstelleBenutzerAsync(string benutzername, string passwort, string anzeigename, BenutzerRolle rolle)
    {
        var benutzer = new Benutzer
        {
            Benutzername = benutzername.ToLower().Trim(),
            PasswortHash = BCrypt.Net.BCrypt.HashPassword(passwort),
            Anzeigename = anzeigename.Trim(),
            Rolle = rolle,
            Aktiv = true,
            ErstelltAm = DateTime.UtcNow
        };

        _db.Benutzer.Add(benutzer);
        await _db.SaveChangesAsync();

        // Bei Rolle Bedienung automatisch Bedienungs-Eintrag anlegen
        if (rolle == BenutzerRolle.Bedienung)
        {
            await SyncBedienungAsync(benutzer);
        }

        return benutzer;
    }

    public async Task<bool> AktualisiereBenutzerAsync(int id, string anzeigename, BenutzerRolle rolle, bool aktiv)
    {
        var benutzer = await _db.Benutzer.FindAsync(id);
        if (benutzer == null) return false;

        var alteRolle = benutzer.Rolle;
        benutzer.Anzeigename = anzeigename.Trim();
        benutzer.Rolle = rolle;
        benutzer.Aktiv = aktiv;
        await _db.SaveChangesAsync();

        // Bedienung synchronisieren
        if (rolle == BenutzerRolle.Bedienung)
        {
            await SyncBedienungAsync(benutzer);
        }
        else if (alteRolle == BenutzerRolle.Bedienung && rolle != BenutzerRolle.Bedienung)
        {
            // Bedienung deaktivieren wenn Rolle gewechselt
            var bedienung = await _db.Bedienungen.FirstOrDefaultAsync(b => b.BenutzerId == id);
            if (bedienung != null)
            {
                bedienung.Aktiv = false;
                await _db.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> PasswortAendernAsync(int id, string neuesPasswort)
    {
        var benutzer = await _db.Benutzer.FindAsync(id);
        if (benutzer == null) return false;

        benutzer.PasswortHash = BCrypt.Net.BCrypt.HashPassword(neuesPasswort);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LoescheBenutzerAsync(int id)
    {
        var benutzer = await _db.Benutzer.FindAsync(id);
        if (benutzer == null) return false;

        // Letzten Admin nicht löschen
        if (benutzer.Rolle == BenutzerRolle.Admin)
        {
            var adminCount = await _db.Benutzer.CountAsync(b => b.Rolle == BenutzerRolle.Admin && b.Aktiv);
            if (adminCount <= 1) return false;
        }

        _db.Benutzer.Remove(benutzer);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task SyncBedienungAsync(Benutzer benutzer)
    {
        var bedienung = await _db.Bedienungen.FirstOrDefaultAsync(b => b.BenutzerId == benutzer.Id);
        if (bedienung == null)
        {
            // Anzeigename in Vor-/Nachname aufteilen
            var teile = benutzer.Anzeigename.Trim().Split(' ', 2);
            var vorname = teile[0];
            var nachname = teile.Length > 1 ? teile[1] : "";

            bedienung = new Bedienung
            {
                Vorname = vorname,
                Nachname = nachname,
                Aktiv = benutzer.Aktiv,
                BenutzerId = benutzer.Id
            };
            _db.Bedienungen.Add(bedienung);
        }
        else
        {
            bedienung.Aktiv = benutzer.Aktiv;
        }
        await _db.SaveChangesAsync();
    }

    // === Einladungs-System ===

    public async Task<BenutzerEinladung> ErstelleEinladungAsync(int benutzerId, int gueltigStunden = 72)
    {
        // Alte unbenutzte Einladungen für diesen Benutzer invalidieren
        var alteEinladungen = await _db.BenutzerEinladungen
            .Where(e => e.BenutzerId == benutzerId && !e.Verwendet)
            .ToListAsync();
        foreach (var alte in alteEinladungen)
            alte.Verwendet = true;

        var einladung = new BenutzerEinladung
        {
            BenutzerId = benutzerId,
            Token = Guid.NewGuid().ToString("N"),
            ErstelltAm = DateTime.UtcNow,
            GueltigBis = DateTime.UtcNow.AddHours(gueltigStunden),
            Verwendet = false
        };

        _db.BenutzerEinladungen.Add(einladung);
        await _db.SaveChangesAsync();
        return einladung;
    }

    public async Task<BenutzerEinladung?> GetGueltigeEinladungAsync(string token)
    {
        return await _db.BenutzerEinladungen
            .FirstOrDefaultAsync(e => e.Token == token
                && !e.Verwendet
                && e.GueltigBis > DateTime.UtcNow);
    }

    public async Task<bool> PasswortPerEinladungSetzenAsync(string token, string neuesPasswort)
    {
        var einladung = await GetGueltigeEinladungAsync(token);
        if (einladung == null) return false;

        var benutzer = await _db.Benutzer.FindAsync(einladung.BenutzerId);
        if (benutzer == null) return false;

        benutzer.PasswortHash = BCrypt.Net.BCrypt.HashPassword(neuesPasswort);
        einladung.Verwendet = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Benutzer?> GetBenutzerByEinladungAsync(string token)
    {
        var einladung = await GetGueltigeEinladungAsync(token);
        if (einladung == null) return null;
        return await _db.Benutzer.FindAsync(einladung.BenutzerId);
    }

    public async Task SeedDefaultAdminAsync()
    {
        if (!await _db.Benutzer.AnyAsync())
        {
            await ErstelleBenutzerAsync("admin", "admin", "Administrator", BenutzerRolle.Admin);
        }
    }

    public static string GetRollenBeschreibung(BenutzerRolle rolle) => rolle switch
    {
        BenutzerRolle.Admin => "Vollzugriff auf alle Funktionen inkl. Benutzerverwaltung",
        BenutzerRolle.Kassenwart => "Kassenbuch, Tresor, SafeBags, Wechselgeld, Übergabe, Veranstaltungen",
        BenutzerRolle.Aufsicht => "Wechselgeld, SafeBags, Tresor-Barbestand, Übergabe, Schichtplan",
        BenutzerRolle.Bedienung => "Verkauf/Bons und Bedienungs-Modus (wird automatisch als Bedienung angelegt)",
        BenutzerRolle.Leserecht => "Nur Lesen (Dashboard, Berichte)",
        _ => ""
    };
}
