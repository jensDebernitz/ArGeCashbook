namespace ArgeKassenbuch.Models;

public class Warengruppe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? VerkaufsstandId { get; set; }
    public bool Aktiv { get; set; } = true;
}
