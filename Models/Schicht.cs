namespace ArgeKassenbuch.Models;

public class Schicht
{
    public int Id { get; set; }
    public int VeranstaltungId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public DateTime Von { get; set; }
    public DateTime Bis { get; set; }
    public string? Rolle { get; set; }
    public string? Bemerkung { get; set; }
}
