namespace ArgeKassenbuch.Models;

public class SafeBag
{
    public int Id { get; set; }
    public int VeranstaltungId { get; set; }
    public string Nummer { get; set; } = string.Empty;
    public decimal Kasseninhalt { get; set; }
    public decimal Wechselgeld { get; set; }
    public decimal Betrag { get; set; }
    public int? VerkaufsstandId { get; set; }
    public string? ErstelltVon { get; set; }
    public DateTime ErstelltAm { get; set; } = DateTime.Now;
    public bool ImTresor { get; set; } = true;
    public string? Bemerkung { get; set; }
}
