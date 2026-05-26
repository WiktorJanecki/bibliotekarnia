using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace bibliotekarnia.ViewModels.Loans;

public class CreateLoanViewModel
{
    [Required]
    [Display(Name = "Book")]
    public int BookId { get; set; }

    [Required]
    [Display(Name = "Member")]
    public int MemberId { get; set; }

    [Required]
    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

    public List<SelectListItem> Books { get; set; } = new();
    public List<SelectListItem> Members { get; set; } = new();
}
