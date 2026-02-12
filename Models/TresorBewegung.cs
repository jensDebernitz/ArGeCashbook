namespace ArgeKassenbuch.Models;

public class TresorBewegung
{
    public int Id { get; set; }
    public int VeranstaltungId { get; set; }
    public TresorBewegungTyp Typ { get; set; }
    public decimal Betrag { get; set; }
    public string? Empfaenger { get; set; }
    public string? Verwendungszweck { get; set; }
    public string? Bemerkung { get; set; }
    public DateTime Zeitpunkt { get; set; } = DateTime.Now;
}

public enum TresorBewegungTyp
{
    Einzahlung,
    Entnahme
}
