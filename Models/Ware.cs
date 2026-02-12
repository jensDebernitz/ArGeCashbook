namespace ArgeKassenbuch.Models;

public class Ware
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Preis { get; set; }
    public int? WarengruppeId { get; set; }
    public int? VerkaufsstandId { get; set; }
    public bool Aktiv { get; set; } = true;
}
