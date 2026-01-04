using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Metadata;
using AuthApp.Data;
using AuthApp.Middleware;
using AuthApp.Models;
using BCrypt.Net;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configuration sources: env vars override appsettings
builder.Configuration.AddEnvironmentVariables();

// Add DB context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                         builder.Configuration["DEFAULT_CONNECTION"] ??
                         "Server=(localdb)\\mssqllocaldb;Database=AuthAppDb;Trusted_Connection=True;"));

// Controllers (AccountController)
builder.Services.AddControllers();

// Register HttpContext accessor for minimal endpoints that may need it
builder.Services.AddHttpContextAccessor();

// Authentication: Cookies + SAML2 (Auth0 as IdP)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Saml2";
})
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        options.LoginPath = "/Account/Login";
    })
    .AddSaml2("Saml2", options =>
    {
        // Read SAML settings from environment variables (set these in production)
        options.SPOptions.EntityId = new EntityId(builder.Configuration["SAML_SP_ENTITYID"] ?? "urn:authapp:sp");

        options.IdentityProviders.Add(new IdentityProvider(
            new EntityId(builder.Configuration["SAML_IDP_ENTITYID"] ?? "https://YOUR_AUTH0_DOMAIN/"), options.SPOptions)
        {
            // metadata location (Auth0 SAML tenant metadata URL) e.g. https://YOUR_DOMAIN/samlp/metadata
            LoadMetadata = true,
            AllowUnsolicitedAuthnResponse = true
        });

        // ACS URL and other settings may be required depending on the IdP config
        // Use persistent cookies issued by the cookie middleware
    });

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/User", "UserPolicy");
    options.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("User", "Admin"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});

// Add rate limiter service state (simple in-memory)
builder.Services.AddSingleton<LoginAttemptTracker>();

var app = builder.Build();

// Ensure DB is migrated and seed an admin user (DEV only — use secure secrets in production)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception migrateEx)
        {
            Console.WriteLine($"Migrate failed: {migrateEx.Message}. Falling back to EnsureCreated().");
            db.Database.EnsureCreated();
        }

        var adminEmail = "admin@local";
        if (!db.Users.Any(u => u.Email == adminEmail))
        {
            var adminPassword = builder.Configuration["ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin123!";
            var admin = new ApplicationUser
            {
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Roles = "Admin,User"
            };
            db.Users.Add(admin);
            db.SaveChanges();
            Console.WriteLine($"Seeded admin user '{adminEmail}' (change password ASAP)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error migrating or seeding database: {ex.Message}");
    }
}

// Enforce HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// Minimal account endpoints for local registration/login (password hashed)
app.MapPost("/local/register", async (AppDbContext db, IHttpContextAccessor http, HttpRequest req) =>
{
    // Not implementing full binding here — see AccountController for full sample
    return Results.BadRequest(new { error = "Use /Account/Register page for registration UI" });
});

app.Run();
