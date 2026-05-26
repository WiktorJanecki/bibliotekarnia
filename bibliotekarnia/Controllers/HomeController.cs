using bibliotekarnia.Data;
using bibliotekarnia.Filters;
using bibliotekarnia.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Controllers;

[ServiceFilter(typeof(RequireLoginFilter))]
public class HomeController : Controller
{
    private readonly LibraryDbContext _db;

    public HomeController(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMonths(-12);

        var totalBooks = await _db.Books.CountAsync();
        var totalMembers = await _db.Members.CountAsync();
        var activeLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null);
        var overdueLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null && l.DueDate < now);

        // Top books: group in SQL with anonymous types, then join + project in memory
        var topBookGroups = await _db.Loans
            .GroupBy(l => l.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var topBookIds = topBookGroups.Select(x => x.BookId).ToList();
        var topBookEntities = await _db.Books
            .Include(b => b.Author)
            .Where(b => topBookIds.Contains(b.Id))
            .ToListAsync();

        var topBooks = topBookGroups
            .Join(topBookEntities, g => g.BookId, b => b.Id,
                (g, b) => new PopularBookItem(b.Id, b.Title, b.Author.LastName + ", " + b.Author.FirstName, g.Count))
            .ToList();

        // Top members: same pattern
        var topMemberGroups = await _db.Loans
            .GroupBy(l => l.MemberId)
            .Select(g => new { MemberId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var topMemberIds = topMemberGroups.Select(x => x.MemberId).ToList();
        var topMemberEntities = await _db.Members
            .Where(m => topMemberIds.Contains(m.Id))
            .ToListAsync();

        var topMembers = topMemberGroups
            .Join(topMemberEntities, g => g.MemberId, m => m.Id,
                (g, m) => new ActiveMemberItem(m.Id, m.FirstName + " " + m.LastName, g.Count))
            .ToList();

        // Genre counts: use anonymous type in SQL, project to record in memory
        var genreRaw = await _db.Books
            .Where(b => b.Genre != null)
            .GroupBy(b => b.Genre!)
            .Select(g => new { Genre = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var genreCounts = genreRaw
            .Select(x => new GenreCountItem(x.Genre, x.Count))
            .ToList();

        var rawMonthly = await _db.Loans
            .Where(l => l.LoanedAt >= cutoff)
            .Select(l => new { l.LoanedAt.Year, l.LoanedAt.Month })
            .ToListAsync();

        var monthlyLoans = rawMonthly
            .GroupBy(x => new { x.Year, x.Month })
            .Select(g => new MonthlyLoanItem(g.Key.Year, g.Key.Month, g.Count()))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        // Overdue list: fetch entity data in SQL, project to record in memory
        var overdueEntities = await _db.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .Where(l => l.ReturnedAt == null && l.DueDate < now)
            .OrderBy(l => l.DueDate)
            .ToListAsync();

        var overdueList = overdueEntities
            .Select(l => new OverdueLoanItem(
                l.Id,
                l.Book.Title,
                l.Member.FirstName + " " + l.Member.LastName,
                l.DueDate,
                (int)(now - l.DueDate).TotalDays))
            .ToList();

        var vm = new DashboardViewModel
        {
            TotalBooks = totalBooks,
            TotalMembers = totalMembers,
            ActiveLoans = activeLoans,
            OverdueLoans = overdueLoans,
            TopBooks = topBooks,
            TopMembers = topMembers,
            GenreCounts = genreCounts,
            MonthlyLoans = monthlyLoans,
            OverdueLoanList = overdueList
        };

        return View(vm);
    }
}
