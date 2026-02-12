using Microsoft.EntityFrameworkCore;
using ArgeKassenbuch.Models;

namespace ArgeKassenbuch.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Verein> Vereine => Set<Verein>();
    public DbSet<Veranstaltung> Veranstaltungen => Set<Veranstaltung>();
    public DbSet<VeranstaltungVerein> VeranstaltungVereine => Set<VeranstaltungVerein>();
    public DbSet<Verkaufsstand> Verkaufsstaende => Set<Verkaufsstand>();
    public DbSet<Warengruppe> Warengruppen => Set<Warengruppe>();
    public DbSet<Ware> Waren => Set<Ware>();
    public DbSet<Bedienung> Bedienungen => Set<Bedienung>();
    public DbSet<Schicht> Schichten => Set<Schicht>();
    public DbSet<SafeBag> SafeBags => Set<SafeBag>();
    public DbSet<Wechselgeld> Wechselgelder => Set<Wechselgeld>();
    public DbSet<Bon> Bons => Set<Bon>();
    public DbSet<BonPosition> BonPositionen => Set<BonPosition>();
    public DbSet<Uebergabeprotokoll> Uebergabeprotokolle => Set<Uebergabeprotokoll>();
    public DbSet<KassenbuchEintrag> KassenbuchEintraege => Set<KassenbuchEintrag>();
    public DbSet<AppEinstellungen> AppEinstellungen => Set<AppEinstellungen>();
    public DbSet<TresorBestand> TresorBestaende => Set<TresorBestand>();
    public DbSet<TresorBewegung> TresorBewegungen => Set<TresorBewegung>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Many-to-many: Veranstaltung <-> Verein
        modelBuilder.Entity<VeranstaltungVerein>()
            .HasKey(vv => new { vv.VeranstaltungId, vv.VereinId });

        modelBuilder.Entity<VeranstaltungVerein>()
            .HasOne(vv => vv.Veranstaltung)
            .WithMany(v => v.VeranstaltungVereine)
            .HasForeignKey(vv => vv.VeranstaltungId);

        modelBuilder.Entity<VeranstaltungVerein>()
            .HasOne(vv => vv.Verein)
            .WithMany()
            .HasForeignKey(vv => vv.VereinId);

        // Bon -> BonPositionen
        modelBuilder.Entity<BonPosition>()
            .HasOne<Bon>()
            .WithMany(b => b.Positionen)
            .HasForeignKey(bp => bp.BonId);

        // Enum als string speichern
        modelBuilder.Entity<Veranstaltung>()
            .Property(v => v.Typ)
            .HasConversion<string>();

        modelBuilder.Entity<Wechselgeld>()
            .Property(w => w.Aktion)
            .HasConversion<string>();

        modelBuilder.Entity<KassenbuchEintrag>()
            .Property(k => k.Typ)
            .HasConversion<string>();

        // Decimal precision
        modelBuilder.Entity<Ware>().Property(w => w.Preis).HasPrecision(10, 2);
        modelBuilder.Entity<SafeBag>().Property(s => s.Betrag).HasPrecision(10, 2);
        modelBuilder.Entity<Wechselgeld>().Property(w => w.Betrag).HasPrecision(10, 2);
        modelBuilder.Entity<Bon>().Property(b => b.Gesamtbetrag).HasPrecision(10, 2);
        modelBuilder.Entity<BonPosition>().Property(bp => bp.Einzelpreis).HasPrecision(10, 2);
        modelBuilder.Entity<Uebergabeprotokoll>().Property(u => u.WechselgeldHoehe).HasPrecision(10, 2);
        modelBuilder.Entity<KassenbuchEintrag>().Property(k => k.Betrag).HasPrecision(10, 2);

        modelBuilder.Entity<TresorBestand>().Property(t => t.Wert).HasPrecision(10, 2);
        modelBuilder.Entity<TresorBewegung>().Property(t => t.Betrag).HasPrecision(10, 2);
        modelBuilder.Entity<TresorBewegung>()
            .Property(t => t.Typ)
            .HasConversion<string>();
    }
}
