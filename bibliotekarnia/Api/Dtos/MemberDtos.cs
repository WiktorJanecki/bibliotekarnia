using System.ComponentModel.DataAnnotations;

namespace bibliotekarnia.Api.Dtos;

public class MemberRequestDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required]
    public DateTime MemberSince { get; set; }
}

public record MemberResponseDto(int Id, string FirstName, string LastName, string Email, string? Phone,
    DateTime MemberSince, int ActiveLoans, int TotalLoans);
