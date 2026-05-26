using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace bibliotekarnia.ViewModels.Books;

public class BookFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ISBN { get; set; }

    [Display(Name = "Published Year")]
    [Range(1000, 2100)]
    public int? PublishedYear { get; set; }

    [MaxLength(100)]
    public string? Genre { get; set; }

    [Required]
    [Range(1, 9999)]
    [Display(Name = "Total Copies")]
    public int TotalCopies { get; set; } = 1;

    [Required]
    [Display(Name = "Author")]
    public int AuthorId { get; set; }

    public List<SelectListItem> Authors { get; set; } = new();
}
