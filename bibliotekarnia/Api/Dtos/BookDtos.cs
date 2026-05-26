using System.ComponentModel.DataAnnotations;

namespace bibliotekarnia.Api.Dtos;

public class BookRequestDto
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ISBN { get; set; }

    public int? PublishedYear { get; set; }

    [MaxLength(100)]
    public string? Genre { get; set; }

    [Required, Range(1, 9999)]
    public int TotalCopies { get; set; } = 1;

    [Required]
    public int AuthorId { get; set; }
}

public record BookResponseDto(int Id, string Title, string? ISBN, int? PublishedYear, string? Genre,
    int TotalCopies, int AvailableCopies, int AuthorId, string AuthorName);
