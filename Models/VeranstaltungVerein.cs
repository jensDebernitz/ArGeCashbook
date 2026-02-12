namespace ArgeKassenbuch.Models;

public class VeranstaltungVerein
{
    public int VeranstaltungId { get; set; }
    public Veranstaltung Veranstaltung { get; set; } = null!;

    public int VereinId { get; set; }
    public Verein Verein { get; set; } = null!;
}
