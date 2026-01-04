using System.ComponentModel.DataAnnotations;

namespace AuthApp.Models;

public class ApplicationUser
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    // Hashed password (bcrypt)
    public string? PasswordHash { get; set; }

    // Comma-separated roles or use normalized design in production
    public string Roles { get; set; } = "User";
}
