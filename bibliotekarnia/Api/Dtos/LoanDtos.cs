using System.ComponentModel.DataAnnotations;

namespace bibliotekarnia.Api.Dtos;

public class LoanRequestDto
{
    [Required]
    public int BookId { get; set; }

    [Required]
    public int MemberId { get; set; }

    [Required]
    public DateTime DueDate { get; set; }
}

public record LoanResponseDto(int Id, int BookId, string BookTitle, int MemberId, string MemberName,
    DateTime LoanedAt, DateTime DueDate, DateTime? ReturnedAt, string Status, string LoanedByUser);
