namespace ArgeKassenbuch.Models;

public class TresorBestand
{
    public int Id { get; set; }
    public int VeranstaltungId { get; set; }
    public string Stueckelung { get; set; } = string.Empty;
    public decimal Wert { get; set; }
    public int Anzahl { get; set; }
    public decimal Gesamt => Wert * Anzahl;
}
