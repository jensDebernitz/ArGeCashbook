namespace ArgeKassenbuch.Models;

public class Veranstaltung
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public VeranstaltungsTyp Typ { get; set; }
    public DateTime Datum { get; set; }
    public DateTime? DatumBis { get; set; }
    public bool Aktiv { get; set; } = true;
    public string? Bemerkung { get; set; }

    public List<VeranstaltungVerein> VeranstaltungVereine { get; set; } = new();
}

public enum VeranstaltungsTyp
{
    Heimatfest,
    Karneval,
    Weinkirmes
}
