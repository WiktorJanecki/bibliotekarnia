using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bibliotekarnia.Models;

public class Loan
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    [Required]
    [Display(Name = "Loaned At")]
    public DateTime LoanedAt { get; set; }

    [Required]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    [Display(Name = "Returned At")]
    public DateTime? ReturnedAt { get; set; }

    public int LoanedByUserId { get; set; }
    public User LoanedByUser { get; set; } = null!;

    [NotMapped]
    public bool IsOverdue => ReturnedAt == null && DateTime.UtcNow > DueDate;

    [NotMapped]
    public bool IsReturned => ReturnedAt.HasValue;

    [NotMapped]
    public string Status => IsReturned ? "Returned" : (IsOverdue ? "Overdue" : "Active");
}
