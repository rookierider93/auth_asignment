# AuthApp — Secure Authentication Assignment

This is a minimal Razor Pages app demonstrating a secure authentication system using:

- Backend: ASP.NET Core (Razor Pages)
- Database: SQL Server (EF Core)
- Authentication: Auth0 (SAML) as IdP + local fallback using bcrypt and cookies

It includes:

- Public page (`/`)
- Protected User page (`/User`)
- Admin-only page (`/Admin`)
- SAML authentication scaffold (Sustainsys.Saml2) integrated with cookie auth
- Local user registration and login for development/testing (bcrypt hashed passwords)
- Simple rate limiting middleware for login endpoints
- Secure cookie flags and environment-based secrets

Security features implemented (choose at least 5):

- Password hashing (bcrypt) for local accounts
- Rate limiting on login endpoints (in-memory IP-based limiter)
- Input validation via data annotations on models and form fields
- CSRF protection: Razor Pages include antiforgery tokens by default for forms
- Secure cookie flags (HttpOnly, Secure, SameSite=Strict)
- Environment-based configuration for secrets (set via environment variables in production)
- Logout & session invalidation via sign-out

Quickstart

1. Requirements:

   - .NET 7 SDK
   - SQL Server / LocalDB

2. From project folder `AuthApp`, restore packages and build:

```powershell
cd c:\ROHAN\Study\auth_asignment\AuthApp
dotnet restore
dotnet build
```

3. Set required environment variables (examples — configure Auth0 first):

PowerShell examples:

```powershell
$env:DEFAULT_CONNECTION = "Server=.;Database=AuthAppDb;User Id=sa;Password=YourStrong!Pass;"
$env:SAML_SP_ENTITYID = "urn:authapp:sp"
$env:SAML_IDP_ENTITYID = "https://YOUR_AUTH0_DOMAIN/"
```

4. Configure Auth0 for SAML:

   - Create a SAML connection/application in Auth0.
   - Set the SP Entity ID to `urn:authapp:sp` (or your chosen value).
   - Set ACS (Assertion Consumer Service) / SAML callback to the metadata endpoint your app exposes. (Using Sustainsys, you will register the SP with the IdP. See Sustainsys docs and Auth0 SAML configuration.)
   - Make sure to include a `roles` attribute (or group) in the SAML assertion that maps to `Role` or custom claim so the app can map Admin/User.

5. Create the database and apply migrations (example using EF Core tools):

```powershell
# (If you add migrations) dotnet ef migrations add Init
# dotnet ef database update
```

Note: This repository now includes a hand-authored initial migration under `Data/Migrations` (file `20260104120000_Init.cs`) and a model snapshot. If you cannot run `dotnet ef` in your environment (SDK/runtime mismatch), you have two options:

- Start the app with `dotnet run --project AuthApp.csproj` — the app will attempt `Migrate()` and will fall back to `EnsureCreated()` to create the schema and seed an `admin@local` account.
- Or apply migrations manually with `dotnet ef database update` (recommended when SDK tooling is available).

If you use the fallback seed, the admin password is taken from the `ADMIN_PASSWORD` environment variable or defaults to `Admin123!` (change immediately in any real environment).

Notes & Next steps

- The SAML configuration requires you to exchange metadata between this SP and Auth0. Use Sustainsys docs: https://github.com/Sustainsys/Saml2
- In production, place secrets (IdP metadata, certificates, SQL credentials) into environment variables or a secure secret store, not `appsettings`.
- Consider using ASP.NET Core Identity for advanced scenarios (user management, tokens, lockout, 2FA).

Files of interest:

- `Program.cs` — wiring for authentication, SAML, cookie options, rate limiter
- `Data/AppDbContext.cs` — EF Core DB context
- `Models/ApplicationUser.cs` — simplified user model with hashed password
- `Middleware/RateLimitingMiddleware.cs` — simple login rate limiter
- `Pages/Index.cshtml`, `Pages/User/Index.cshtml`, `Pages/Admin/Index.cshtml` — public/protected pages
- `Controllers/AccountController.cs` — triggers SAML challenge, local login/register/logout

If you want, I can:

- Add EF Core migrations and a small seed (admin user)
- Add unit tests for the middleware and controllers
- Configure Sustainsys metadata endpoint and show a sample Auth0 settings file
