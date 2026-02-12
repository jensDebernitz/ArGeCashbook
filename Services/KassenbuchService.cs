using Microsoft.EntityFrameworkCore;
using ArgeKassenbuch.Data;
using ArgeKassenbuch.Models;

namespace ArgeKassenbuch.Services;

public class KassenbuchService
{
    private readonly AppDbContext _db;

    public KassenbuchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SeedDefaultDataAsync()
    {
        if (!await _db.Vereine.AnyAsync())
        {
            _db.Vereine.AddRange(
                new Verein { Name = "Feuerwehrkameradschaft", Kuerzel = "FK" },
                new Verein { Name = "Sportverein", Kuerzel = "SV" },
                new Verein { Name = "Karnevalsverein", Kuerzel = "KV" },
                new Verein { Name = "Heimat- und Verkehrsverein", Kuerzel = "HVV" }
            );
        }

        if (!await _db.AppEinstellungen.AnyAsync())
        {
            _db.AppEinstellungen.Add(new AppEinstellungen());
        }

        await _db.SaveChangesAsync();
    }

    // === Vereine ===
    public async Task<List<Verein>> GetVereineAsync()
        => await _db.Vereine.ToListAsync();

    // === Veranstaltungen ===
    public async Task<List<Veranstaltung>> GetVeranstaltungenAsync()
        => await _db.Veranstaltungen.Include(v => v.VeranstaltungVereine).ToListAsync();

    public async Task<Veranstaltung?> GetVeranstaltungAsync(int id)
        => await _db.Veranstaltungen.Include(v => v.VeranstaltungVereine).FirstOrDefaultAsync(v => v.Id == id);

    public async Task CreateVeranstaltungAsync(Veranstaltung veranstaltung, List<int> vereinIds)
    {
        _db.Veranstaltungen.Add(veranstaltung);
        await _db.SaveChangesAsync();
        foreach (var vid in vereinIds)
            _db.VeranstaltungVereine.Add(new VeranstaltungVerein { VeranstaltungId = veranstaltung.Id, VereinId = vid });
        await _db.SaveChangesAsync();
    }

    public async Task UpdateVeranstaltungAsync(Veranstaltung veranstaltung, List<int> vereinIds)
    {
        _db.Veranstaltungen.Update(veranstaltung);

        var existing = await _db.VeranstaltungVereine.Where(vv => vv.VeranstaltungId == veranstaltung.Id).ToListAsync();
        _db.VeranstaltungVereine.RemoveRange(existing);
        foreach (var vid in vereinIds)
            _db.VeranstaltungVereine.Add(new VeranstaltungVerein { VeranstaltungId = veranstaltung.Id, VereinId = vid });
        await _db.SaveChangesAsync();
    }

    public async Task DeleteVeranstaltungAsync(int id)
    {
        var entity = await _db.Veranstaltungen.FindAsync(id);
        if (entity != null)
        {
            _db.Veranstaltungen.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }

    // === Verkaufsstaende ===
    public async Task<List<Verkaufsstand>> GetVerkaufsstaendeAsync()
        => await _db.Verkaufsstaende.ToListAsync();

    // === Warengruppen ===
    public async Task<List<Warengruppe>> GetWarengruppenAsync()
        => await _db.Warengruppen.ToListAsync();

    // === Waren ===
    public async Task<List<Ware>> GetWarenAsync()
        => await _db.Waren.ToListAsync();

    public async Task<List<Ware>> GetWarenByStandAsync(int verkaufsstandId)
        => await _db.Waren.Where(w => w.VerkaufsstandId == verkaufsstandId && w.Aktiv).ToListAsync();

    // === Bedienungen ===
    public async Task<List<Bedienung>> GetBedienungenAsync()
        => await _db.Bedienungen.ToListAsync();

    // === Schichten ===
    public async Task<List<Schicht>> GetSchichtenAsync(int? veranstaltungId = null)
    {
        var query = _db.Schichten.AsQueryable();
        if (veranstaltungId.HasValue)
            query = query.Where(s => s.VeranstaltungId == veranstaltungId.Value);
        return await query.ToListAsync();
    }

    // === SafeBags ===
    public async Task<List<SafeBag>> GetSafeBagsAsync(int? veranstaltungId = null)
    {
        var query = _db.SafeBags.AsQueryable();
        if (veranstaltungId.HasValue)
            query = query.Where(s => s.VeranstaltungId == veranstaltungId.Value);
        return await query.ToListAsync();
    }

    public async Task<decimal> GetSafeBagGesamtAsync(int veranstaltungId)
        => await _db.SafeBags.Where(s => s.VeranstaltungId == veranstaltungId && s.ImTresor).SumAsync(s => s.Betrag);

    // === Wechselgeld ===
    public async Task<List<Wechselgeld>> GetWechselgeldAsync(int? veranstaltungId = null)
    {
        var query = _db.Wechselgelder.AsQueryable();
        if (veranstaltungId.HasValue)
            query = query.Where(w => w.VeranstaltungId == veranstaltungId.Value);
        return await query.ToListAsync();
    }

    // === Bons ===
    public async Task<List<Bon>> GetBonsAsync(int? veranstaltungId = null)
    {
        var query = _db.Bons.Include(b => b.Positionen).AsQueryable();
        if (veranstaltungId.HasValue)
            query = query.Where(b => b.VeranstaltungId == veranstaltungId.Value);
        return await query.ToListAsync();
    }

    public async Task<int> GetNaechsteBonNummerAsync(int veranstaltungId)
    {
        var max = await _db.Bons.Where(b => b.VeranstaltungId == veranstaltungId)
            .MaxAsync(b => (int?)b.BonNummer);
        return (max ?? 0) + 1;
    }

    // === Uebergabeprotokolle ===
    public async Task<List<Uebergabeprotokoll>> GetUebergabeprotokolleAsync(int? veranstaltungId = null)
    {
        var query = _db.Uebergabeprotokolle.AsQueryable();
        if (veranstaltungId.HasValue)
            query = query.Where(u => u.VeranstaltungId == veranstaltungId.Value);
        return await query.ToListAsync();
    }

    // === KassenbuchEintraege ===
    public async Task<List<KassenbuchEintrag>> GetKassenbuchEintraegeAsync(int? veranstaltungId = null)
    {
        var query = _db.KassenbuchEintraege.AsQueryable();
        if (veranstaltungId.HasValue)
            query = query.Where(k => k.VeranstaltungId == veranstaltungId.Value);
        return await query.ToListAsync();
    }

    // === Tresor-Barbestand ===
    public async Task<List<TresorBestand>> GetTresorBestandAsync(int veranstaltungId)
        => await _db.TresorBestaende.Where(t => t.VeranstaltungId == veranstaltungId).ToListAsync();

    public async Task<List<TresorBewegung>> GetTresorBewegungenAsync(int veranstaltungId)
        => await _db.TresorBewegungen.Where(t => t.VeranstaltungId == veranstaltungId)
            .OrderByDescending(t => t.Zeitpunkt).ToListAsync();

    public async Task<decimal> GetTresorSaldoAsync(int veranstaltungId)
    {
        var einzahlungen = await _db.TresorBewegungen
            .Where(t => t.VeranstaltungId == veranstaltungId && t.Typ == TresorBewegungTyp.Einzahlung)
            .SumAsync(t => (decimal?)t.Betrag) ?? 0;
        var entnahmen = await _db.TresorBewegungen
            .Where(t => t.VeranstaltungId == veranstaltungId && t.Typ == TresorBewegungTyp.Entnahme)
            .SumAsync(t => (decimal?)t.Betrag) ?? 0;
        return einzahlungen - entnahmen;
    }

    // === Einstellungen ===
    public async Task<AppEinstellungen> GetEinstellungenAsync()
        => await _db.AppEinstellungen.FirstOrDefaultAsync() ?? new AppEinstellungen();

    // === Generische CRUD ===
    public async Task<T> AddAsync<T>(T entity) where T : class
    {
        _db.Set<T>().Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<T> UpdateAsync<T>(T entity) where T : class
    {
        _db.Set<T>().Update(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync<T>(int id) where T : class
    {
        var entity = await _db.Set<T>().FindAsync(id);
        if (entity != null)
        {
            _db.Set<T>().Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
