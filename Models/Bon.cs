using System.ComponentModel.DataAnnotations.Schema;

namespace ArgeKassenbuch.Models;

public class Bon
{
    public int Id { get; set; }
    public int VeranstaltungId { get; set; }
    public int? BedienungId { get; set; }
    public int? VerkaufsstandId { get; set; }
    public int BonNummer { get; set; }
    public List<BonPosition> Positionen { get; set; } = new();
    public decimal Gesamtbetrag { get; set; }
    public DateTime ErstelltAm { get; set; } = DateTime.Now;
    public bool Storniert { get; set; }
    public string? Bemerkung { get; set; }
}

public class BonPosition
{
    public int Id { get; set; }
    public int BonId { get; set; }
    public int WareId { get; set; }
    public string WareName { get; set; } = string.Empty;
    public int Anzahl { get; set; }
    public decimal Einzelpreis { get; set; }

    [NotMapped]
    public decimal Gesamtpreis => Anzahl * Einzelpreis;
}
