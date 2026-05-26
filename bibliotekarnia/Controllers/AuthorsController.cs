using bibliotekarnia.Data;
using bibliotekarnia.Filters;
using bibliotekarnia.Models;
using bibliotekarnia.ViewModels.Authors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Controllers;

[ServiceFilter(typeof(RequireLoginFilter))]
public class AuthorsController : Controller
{
    private readonly LibraryDbContext _db;

    public AuthorsController(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var authors = await _db.Authors
            .Include(a => a.Books)
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync();
        return View(authors);
    }

    public async Task<IActionResult> Details(int id)
    {
        var author = await _db.Authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (author == null) return NotFound();
        return View(author);
    }

    public IActionResult Create() => View(new AuthorFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuthorFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        _db.Authors.Add(new Author
        {
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            BirthYear = vm.BirthYear,
            Nationality = vm.Nationality
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Author created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var author = await _db.Authors.FindAsync(id);
        if (author == null) return NotFound();

        return View(new AuthorFormViewModel
        {
            Id = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            BirthYear = author.BirthYear,
            Nationality = author.Nationality
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AuthorFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var author = await _db.Authors.FindAsync(id);
        if (author == null) return NotFound();

        author.FirstName = vm.FirstName;
        author.LastName = vm.LastName;
        author.BirthYear = vm.BirthYear;
        author.Nationality = vm.Nationality;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Author updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var author = await _db.Authors.Include(a => a.Books).FirstOrDefaultAsync(a => a.Id == id);
        if (author == null) return NotFound();

        if (author.Books.Any())
        {
            TempData["Error"] = "Cannot delete author with existing books. Remove the books first.";
            return RedirectToAction(nameof(Index));
        }

        _db.Authors.Remove(author);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Author deleted.";
        return RedirectToAction(nameof(Index));
    }
}
