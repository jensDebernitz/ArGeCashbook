namespace ArgeKassenbuch.Models;

public class Uebergabeprotokoll
{
    public int Id { get; set; }
    public int VeranstaltungId { get; set; }
    public DateTime Zeitpunkt { get; set; } = DateTime.Now;
    public string UebergebenVon { get; set; } = string.Empty;
    public string UebergebenAn { get; set; } = string.Empty;
    public int AnzahlSafeBags { get; set; }
    public decimal WechselgeldHoehe { get; set; }
    public bool SchluesselUebergeben { get; set; }
    public string? Bemerkung { get; set; }
    public string? UnterschriftVon { get; set; }
    public string? UnterschriftAn { get; set; }
}
