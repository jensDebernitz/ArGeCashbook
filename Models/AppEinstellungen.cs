namespace ArgeKassenbuch.Models;

public class AppEinstellungen
{
    public int Id { get; set; }
    public string AdminPasswort { get; set; } = "admin";
    public int? AktiveVeranstaltungId { get; set; }
    public string OrganisationsName { get; set; } = "ArGe Oberfell";
}
