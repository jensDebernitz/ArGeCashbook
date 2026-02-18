using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using ArgeKassenbuch.Components;
using ArgeKassenbuch.Data;
using ArgeKassenbuch.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL konfigurieren
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=arge_kassenbuch;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<KassenbuchService>();
builder.Services.AddScoped<BenutzerService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });
builder.WebHost.UseUrls("http://localhost:8000");
builder.Services.AddCascadingAuthenticationState();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Datenbank initialisieren
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var service = scope.ServiceProvider.GetRequiredService<KassenbuchService>();
    await service.SeedDefaultDataAsync();
    var benutzerService = scope.ServiceProvider.GetRequiredService<BenutzerService>();
    await benutzerService.SeedDefaultAdminAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Login endpoint
app.MapPost("/api/auth/login", async (HttpContext context, BenutzerService benutzerService) =>
{
    var form = await context.Request.ReadFormAsync();
    var benutzername = form["benutzername"].ToString();
    var passwort = form["passwort"].ToString();

    var benutzer = await benutzerService.LoginAsync(benutzername, passwort);

    if (benutzer != null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, benutzer.Anzeigename),
            new(ClaimTypes.NameIdentifier, benutzer.Id.ToString()),
            new(ClaimTypes.Role, benutzer.Rolle.ToString()),
            new("Benutzername", benutzer.Benutzername)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        context.Response.Redirect("/");
    }
    else
    {
        context.Response.Redirect("/login?error=1");
    }
});

// Logout endpoint
app.MapGet("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
