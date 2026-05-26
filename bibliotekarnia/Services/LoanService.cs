using bibliotekarnia.Data;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Services;

public class LoanService
{
    private readonly LibraryDbContext _db;

    public LoanService(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetAvailableCopiesAsync(int bookId)
    {
        var book = await _db.Books.FindAsync(bookId);
        if (book == null) return 0;

        var activeLoans = await _db.Loans
            .CountAsync(l => l.BookId == bookId && l.ReturnedAt == null);

        return Math.Max(0, book.TotalCopies - activeLoans);
    }
}
