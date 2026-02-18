namespace ArgeKassenbuch.Models;

public enum BenutzerRolle
{
    Admin,
    Kassenwart,
    Aufsicht,
    Bedienung,
    Leserecht
}

public class Benutzer
{
    public int Id { get; set; }
    public string Benutzername { get; set; } = string.Empty;
    public string PasswortHash { get; set; } = string.Empty;
    public string Anzeigename { get; set; } = string.Empty;
    public BenutzerRolle Rolle { get; set; } = BenutzerRolle.Leserecht;
    public bool Aktiv { get; set; } = true;
    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
    public DateTime? LetzterLogin { get; set; }
}
