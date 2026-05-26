using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bibliotekarnia.Models;

public class Author
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
    public int? BirthYear { get; set; }

    [MaxLength(100)]
    public string? Nationality { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
