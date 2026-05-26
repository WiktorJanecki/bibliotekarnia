using System.ComponentModel.DataAnnotations;

namespace bibliotekarnia.Api.Dtos;

public class AuthorRequestDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public int? BirthYear { get; set; }

    [MaxLength(100)]
    public string? Nationality { get; set; }
}

public record AuthorResponseDto(int Id, string FirstName, string LastName, int? BirthYear, string? Nationality, int BookCount);
public record AuthorDetailDto(int Id, string FirstName, string LastName, int? BirthYear, string? Nationality, List<BookSummaryDto> Books);
public record BookSummaryDto(int Id, string Title, string? Genre, int? PublishedYear, int TotalCopies, int AvailableCopies);
