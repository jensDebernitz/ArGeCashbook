using System.ComponentModel.DataAnnotations.Schema;

namespace ArgeKassenbuch.Models;

public class Bedienung
{
    public int Id { get; set; }
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public bool Aktiv { get; set; } = true;
    public int? BenutzerId { get; set; }

    [NotMapped]
    public string VollerName => $"{Vorname} {Nachname}";
}
