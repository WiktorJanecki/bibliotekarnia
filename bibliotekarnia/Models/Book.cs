using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bibliotekarnia.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ISBN { get; set; }

    [Display(Name = "Published Year")]
    public int? PublishedYear { get; set; }

    [MaxLength(100)]
    public string? Genre { get; set; }

    [Required]
    [Range(1, 9999)]
    [Display(Name = "Total Copies")]
    public int TotalCopies { get; set; } = 1;

    [Display(Name = "Author")]
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();

    [NotMapped]
    public int AvailableCopies { get; set; }
}
