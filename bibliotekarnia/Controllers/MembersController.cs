using bibliotekarnia.Data;
using bibliotekarnia.Filters;
using bibliotekarnia.Models;
using bibliotekarnia.ViewModels.Members;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Controllers;

[ServiceFilter(typeof(RequireLoginFilter))]
public class MembersController : Controller
{
    private readonly LibraryDbContext _db;

    public MembersController(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var members = await _db.Members
            .Include(m => m.Loans)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();
        return View(members);
    }

    public async Task<IActionResult> Details(int id)
    {
        var member = await _db.Members
            .Include(m => m.Loans).ThenInclude(l => l.Book).ThenInclude(b => b.Author)
            .Include(m => m.Loans).ThenInclude(l => l.LoanedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return NotFound();
        return View(member);
    }

    public IActionResult Create() => View(new MemberFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var exists = await _db.Members.AnyAsync(m => m.Email == vm.Email);
        if (exists)
        {
            ModelState.AddModelError("Email", "A member with this email already exists.");
            return View(vm);
        }

        _db.Members.Add(new Member
        {
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            Email = vm.Email,
            Phone = vm.Phone,
            MemberSince = vm.MemberSince.ToUniversalTime()
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Member registered.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var member = await _db.Members.FindAsync(id);
        if (member == null) return NotFound();

        return View(new MemberFormViewModel
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Phone = member.Phone,
            MemberSince = member.MemberSince
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MemberFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var member = await _db.Members.FindAsync(id);
        if (member == null) return NotFound();

        var exists = await _db.Members.AnyAsync(m => m.Email == vm.Email && m.Id != id);
        if (exists)
        {
            ModelState.AddModelError("Email", "A member with this email already exists.");
            return View(vm);
        }

        member.FirstName = vm.FirstName;
        member.LastName = vm.LastName;
        member.Email = vm.Email;
        member.Phone = vm.Phone;
        member.MemberSince = vm.MemberSince.ToUniversalTime();
        await _db.SaveChangesAsync();
        TempData["Success"] = "Member updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var hasActiveLoans = await _db.Loans.AnyAsync(l => l.MemberId == id && l.ReturnedAt == null);
        if (hasActiveLoans)
        {
            TempData["Error"] = "Cannot delete a member with active loans.";
            return RedirectToAction(nameof(Index));
        }

        var member = await _db.Members.FindAsync(id);
        if (member == null) return NotFound();

        _db.Members.Remove(member);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Member deleted.";
        return RedirectToAction(nameof(Index));
    }
}
