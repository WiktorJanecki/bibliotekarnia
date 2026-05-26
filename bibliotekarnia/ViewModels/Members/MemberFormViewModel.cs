using System.ComponentModel.DataAnnotations;

namespace bibliotekarnia.ViewModels.Members;

public class MemberFormViewModel
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

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required]
    [Display(Name = "Member Since")]
    [DataType(DataType.Date)]
    public DateTime MemberSince { get; set; } = DateTime.Today;
}
