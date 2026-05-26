using bibliotekarnia.Data;
using bibliotekarnia.Filters;
using bibliotekarnia.Models;
using bibliotekarnia.Services;
using bibliotekarnia.ViewModels.Books;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Controllers;

[ServiceFilter(typeof(RequireLoginFilter))]
public class BooksController : Controller
{
    private readonly LibraryDbContext _db;
    private readonly LoanService _loanService;

    public BooksController(LibraryDbContext db, LoanService loanService)
    {
        _db = db;
        _loanService = loanService;
    }

    public async Task<IActionResult> Index(string? genre)
    {
        var query = _db.Books.Include(b => b.Author).AsQueryable();
        if (!string.IsNullOrEmpty(genre))
            query = query.Where(b => b.Genre == genre);

        var books = await query.OrderBy(b => b.Title).ToListAsync();

        var activeLoansPerBook = await _db.Loans
            .Where(l => l.ReturnedAt == null)
            .GroupBy(l => l.BookId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        foreach (var book in books)
            book.AvailableCopies = Math.Max(0, book.TotalCopies - (activeLoansPerBook.GetValueOrDefault(book.Id, 0)));

        var genres = await _db.Books
            .Where(b => b.Genre != null)
            .Select(b => b.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        ViewBag.Genres = genres;
        ViewBag.SelectedGenre = genre;
        return View(books);
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books
            .Include(b => b.Author)
            .Include(b => b.Loans).ThenInclude(l => l.Member)
            .Include(b => b.Loans).ThenInclude(l => l.LoanedByUser)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound();

        book.AvailableCopies = await _loanService.GetAvailableCopiesAsync(id);
        return View(book);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new BookFormViewModel
        {
            Authors = await GetAuthorSelectList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Authors = await GetAuthorSelectList();
            return View(vm);
        }

        _db.Books.Add(new Book
        {
            Title = vm.Title,
            ISBN = vm.ISBN,
            PublishedYear = vm.PublishedYear,
            Genre = vm.Genre,
            TotalCopies = vm.TotalCopies,
            AuthorId = vm.AuthorId
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Book added successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        return View(new BookFormViewModel
        {
            Id = book.Id,
            Title = book.Title,
            ISBN = book.ISBN,
            PublishedYear = book.PublishedYear,
            Genre = book.Genre,
            TotalCopies = book.TotalCopies,
            AuthorId = book.AuthorId,
            Authors = await GetAuthorSelectList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Authors = await GetAuthorSelectList();
            return View(vm);
        }

        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        var activeLoans = await _db.Loans.CountAsync(l => l.BookId == id && l.ReturnedAt == null);
        if (vm.TotalCopies < activeLoans)
        {
            ModelState.AddModelError("TotalCopies", $"Cannot set copies below active loans count ({activeLoans}).");
            vm.Authors = await GetAuthorSelectList();
            return View(vm);
        }

        book.Title = vm.Title;
        book.ISBN = vm.ISBN;
        book.PublishedYear = vm.PublishedYear;
        book.Genre = vm.Genre;
        book.TotalCopies = vm.TotalCopies;
        book.AuthorId = vm.AuthorId;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Book updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var activeLoans = await _db.Loans.AnyAsync(l => l.BookId == id && l.ReturnedAt == null);
        if (activeLoans)
        {
            TempData["Error"] = "Cannot delete a book with active loans.";
            return RedirectToAction(nameof(Index));
        }

        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Book deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetAuthorSelectList()
    {
        return await _db.Authors
            .OrderBy(a => a.LastName)
            .Select(a => new SelectListItem($"{a.LastName}, {a.FirstName}", a.Id.ToString()))
            .ToListAsync();
    }
}
