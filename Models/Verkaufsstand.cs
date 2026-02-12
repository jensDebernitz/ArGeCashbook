namespace ArgeKassenbuch.Models;

public class Verkaufsstand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Beschreibung { get; set; }
    public bool Aktiv { get; set; } = true;
}
