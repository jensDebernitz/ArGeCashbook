namespace ArgeKassenbuch.Models;

public class Verein
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kuerzel { get; set; } = string.Empty;
    public bool Aktiv { get; set; } = true;
}
