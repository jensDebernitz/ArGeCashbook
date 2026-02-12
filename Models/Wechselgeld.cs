namespace ArgeKassenbuch.Models;

public class Wechselgeld
{
    public int Id { get; set; }
    public int VeranstaltungId { get; set; }
    public WechselgeldAktion Aktion { get; set; }
    public decimal Betrag { get; set; }
    public string? EmpfaengerName { get; set; }
    public int? BedienungId { get; set; }
    public int? VerkaufsstandId { get; set; }
    public DateTime Zeitpunkt { get; set; } = DateTime.Now;
    public string? Bemerkung { get; set; }
}

public enum WechselgeldAktion
{
    Ausgabe,
    Ruecknahme
}
