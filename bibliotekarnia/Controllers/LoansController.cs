using bibliotekarnia.Data;
using bibliotekarnia.Filters;
using bibliotekarnia.Models;
using bibliotekarnia.Services;
using bibliotekarnia.ViewModels.Loans;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Controllers;

[ServiceFilter(typeof(RequireLoginFilter))]
public class LoansController : Controller
{
    private readonly LibraryDbContext _db;
    private readonly LoanService _loanService;

    public LoansController(LibraryDbContext db, LoanService loanService)
    {
        _db = db;
        _loanService = loanService;
    }

    public async Task<IActionResult> Index(string? filter)
    {
        var now = DateTime.UtcNow;
        var query = _db.Loans
            .Include(l => l.Book).ThenInclude(b => b.Author)
            .Include(l => l.Member)
            .Include(l => l.LoanedByUser)
            .AsQueryable();

        query = filter switch
        {
            "active" => query.Where(l => l.ReturnedAt == null && l.DueDate >= now),
            "overdue" => query.Where(l => l.ReturnedAt == null && l.DueDate < now),
            "returned" => query.Where(l => l.ReturnedAt != null),
            _ => query
        };

        var loans = await query.OrderByDescending(l => l.LoanedAt).ToListAsync();
        ViewBag.Filter = filter ?? "all";
        return View(loans);
    }

    public async Task<IActionResult> Create(int? bookId, int? memberId)
    {
        var vm = new CreateLoanViewModel
        {
            DueDate = DateTime.Today.AddDays(14)
        };
        if (bookId.HasValue) vm.BookId = bookId.Value;
        if (memberId.HasValue) vm.MemberId = memberId.Value;

        vm.Books = await GetAvailableBookSelectList();
        vm.Members = await GetMemberSelectList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLoanViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Books = await GetAvailableBookSelectList();
            vm.Members = await GetMemberSelectList();
            return View(vm);
        }

        var available = await _loanService.GetAvailableCopiesAsync(vm.BookId);
        if (available <= 0)
        {
            ModelState.AddModelError("BookId", "No copies available for this book.");
            vm.Books = await GetAvailableBookSelectList();
            vm.Members = await GetMemberSelectList();
            return View(vm);
        }

        if (vm.DueDate <= DateTime.Today)
        {
            ModelState.AddModelError("DueDate", "Due date must be in the future.");
            vm.Books = await GetAvailableBookSelectList();
            vm.Members = await GetMemberSelectList();
            return View(vm);
        }

        var userId = int.Parse(HttpContext.Session.GetString("UserId")!);

        _db.Loans.Add(new Loan
        {
            BookId = vm.BookId,
            MemberId = vm.MemberId,
            LoanedAt = DateTime.UtcNow,
            DueDate = vm.DueDate.ToUniversalTime(),
            LoanedByUserId = userId
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Loan created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var loan = await _db.Loans
            .Include(l => l.Book).ThenInclude(b => b.Author)
            .Include(l => l.Member)
            .Include(l => l.LoanedByUser)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound();
        return View(loan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        var loan = await _db.Loans.FindAsync(id);
        if (loan == null) return NotFound();
        if (loan.ReturnedAt.HasValue)
        {
            TempData["Error"] = "This loan has already been returned.";
            return RedirectToAction(nameof(Details), new { id });
        }

        loan.ReturnedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Book returned successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetAvailableBookSelectList()
    {
        var activeLoans = await _db.Loans
            .Where(l => l.ReturnedAt == null)
            .GroupBy(l => l.BookId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return await _db.Books
            .Include(b => b.Author)
            .OrderBy(b => b.Title)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.TotalCopies,
                Author = b.Author.LastName + ", " + b.Author.FirstName
            })
            .ToListAsync()
            .ContinueWith(t => t.Result
                .Select(b =>
                {
                    var available = b.TotalCopies - activeLoans.GetValueOrDefault(b.Id, 0);
                    return new SelectListItem(
                        $"{b.Title} — {b.Author} ({available} available)",
                        b.Id.ToString(),
                        false,
                        available <= 0
                    );
                })
                .ToList());
    }

    private async Task<List<SelectListItem>> GetMemberSelectList()
    {
        return await _db.Members
            .OrderBy(m => m.LastName)
            .Select(m => new SelectListItem($"{m.LastName}, {m.FirstName} ({m.Email})", m.Id.ToString()))
            .ToListAsync();
    }
}
