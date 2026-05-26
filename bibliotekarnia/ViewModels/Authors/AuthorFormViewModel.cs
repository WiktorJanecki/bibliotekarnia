using System.ComponentModel.DataAnnotations;

namespace bibliotekarnia.ViewModels.Authors;

public class AuthorFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Birth Year")]
    [Range(0, 2100)]
    public int? BirthYear { get; set; }

    [MaxLength(100)]
    public string? Nationality { get; set; }
}
