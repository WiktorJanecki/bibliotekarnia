using System.ComponentModel.DataAnnotations;

namespace bibliotekarnia.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string ApiToken { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Loan> ProcessedLoans { get; set; } = new List<Loan>();
}
