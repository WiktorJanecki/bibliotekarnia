using bibliotekarnia.Data;
using bibliotekarnia.Filters;
using bibliotekarnia.Models;
using bibliotekarnia.ViewModels.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Controllers;

[ServiceFilter(typeof(RequireAdminFilter))]
public class UsersController : Controller
{
    private readonly LibraryDbContext _db;

    public UsersController(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _db.Users.OrderBy(u => u.Username).ToListAsync();
        return View(users);
    }

    public IActionResult Create() => View(new CreateUserViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var exists = await _db.Users.AnyAsync(u => u.Username == vm.Username);
        if (exists)
        {
            ModelState.AddModelError("Username", "Username already taken.");
            return View(vm);
        }

        _db.Users.Add(new User
        {
            Username = vm.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
            ApiToken = Guid.NewGuid().ToString("N"),
            IsAdmin = vm.IsAdmin,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"User '{vm.Username}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = int.Parse(HttpContext.Session.GetString("UserId")!);
        if (id == currentUserId)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var adminCount = await _db.Users.CountAsync(u => u.IsAdmin);
        var target = await _db.Users.FindAsync(id);
        if (target == null) return NotFound();

        if (target.IsAdmin && adminCount <= 1)
        {
            TempData["Error"] = "Cannot delete the last admin account.";
            return RedirectToAction(nameof(Index));
        }

        _db.Users.Remove(target);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"User '{target.Username}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateToken(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.ApiToken = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync();
        TempData["Success"] = $"API token for '{user.Username}' regenerated.";
        return RedirectToAction(nameof(Index));
    }
}
