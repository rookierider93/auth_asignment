using System.Security.Claims;
using AuthApp.Data;
using AuthApp.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AuthApp.Controllers;

[Route("Account")]
public class AccountController : Controller
{
    private readonly AppDbContext _db;

    public AccountController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("ExternalLogin")]
    public IActionResult ExternalLogin()
    {
        // Trigger SAML2 challenge
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "Saml2");
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> Logout()
    {
        // Sign out cookie and invalidate session
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "");
    }

    [HttpGet("Register")]
    public IActionResult Register()
    {
        return View(); // For brevity, not adding a view; use API or Postman to register in dev
    }

    [HttpPost("Register")]
    public async Task<IActionResult> RegisterPost([FromForm] string email, [FromForm] string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest("Email and password required");

        if (_db.Users.Any(u => u.Email == email))
            return Conflict("User already exists");

        var user = new ApplicationUser
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Roles = "User"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Registered" });
    }

    [HttpPost("LocalLogin")]
    public async Task<IActionResult> LocalLogin([FromForm] string email, [FromForm] string password)
    {
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return Unauthorized();

        if (user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Unauthorized();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        foreach (var role in user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new { message = "Logged in" });
    }
}
