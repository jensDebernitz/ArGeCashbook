namespace ArgeKassenbuch.Models;

public class KassenbuchEintrag
{
    public int Id { get; set; }
    public int VeranstaltungId { get; set; }
    public KassenbuchTyp Typ { get; set; }
    public decimal Betrag { get; set; }
    public string Beschreibung { get; set; } = string.Empty;
    public int? ReferenzId { get; set; }
    public DateTime Zeitpunkt { get; set; } = DateTime.Now;
    public string? ErstelltVon { get; set; }
}

public enum KassenbuchTyp
{
    Einnahme,
    Ausgabe,
    WechselgeldAusgabe,
    WechselgeldRuecknahme,
    SafeBagEinlage
}
