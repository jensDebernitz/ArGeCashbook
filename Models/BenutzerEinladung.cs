namespace ArgeKassenbuch.Models;

public class BenutzerEinladung
{
    public int Id { get; set; }
    public int BenutzerId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
    public DateTime GueltigBis { get; set; }
    public bool Verwendet { get; set; } = false;
}
