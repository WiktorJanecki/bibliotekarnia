using bibliotekarnia.Api.Dtos;
using bibliotekarnia.Data;
using bibliotekarnia.Models;
using bibliotekarnia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Api;

[ApiController]
[Route("api/loans")]
public class LoansApiController : ControllerBase
{
    private readonly LibraryDbContext _db;
    private readonly LoanService _loanService;

    public LoansApiController(LibraryDbContext db, LoanService loanService)
    {
        _db = db;
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var now = DateTime.UtcNow;
        var query = _db.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .Include(l => l.LoanedByUser)
            .AsQueryable();

        query = status?.ToLower() switch
        {
            "active" => query.Where(l => l.ReturnedAt == null && l.DueDate >= now),
            "overdue" => query.Where(l => l.ReturnedAt == null && l.DueDate < now),
            "returned" => query.Where(l => l.ReturnedAt != null),
            _ => query
        };

        var loans = await query.OrderByDescending(l => l.LoanedAt).ToListAsync();
        return Ok(loans.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var loan = await _db.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .Include(l => l.LoanedByUser)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound(new { error = "Loan not found." });
        return Ok(ToDto(loan));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LoanRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (dto.DueDate <= DateTime.UtcNow) return BadRequest(new { error = "Due date must be in the future." });

        var available = await _loanService.GetAvailableCopiesAsync(dto.BookId);
        if (available <= 0) return BadRequest(new { error = "No copies available for this book." });

        var bookExists = await _db.Books.AnyAsync(b => b.Id == dto.BookId);
        if (!bookExists) return BadRequest(new { error = "Book not found." });

        var memberExists = await _db.Members.AnyAsync(m => m.Id == dto.MemberId);
        if (!memberExists) return BadRequest(new { error = "Member not found." });

        var apiUser = (Models.User)HttpContext.Items["ApiUser"]!;

        var loan = new Loan
        {
            BookId = dto.BookId,
            MemberId = dto.MemberId,
            LoanedAt = DateTime.UtcNow,
            DueDate = dto.DueDate.ToUniversalTime(),
            LoanedByUserId = apiUser.Id
        };
        _db.Loans.Add(loan);
        await _db.SaveChangesAsync();

        await _db.Entry(loan).Reference(l => l.Book).LoadAsync();
        await _db.Entry(loan).Reference(l => l.Member).LoadAsync();
        await _db.Entry(loan).Reference(l => l.LoanedByUser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, ToDto(loan));
    }

    [HttpPut("{id}/return")]
    public async Task<IActionResult> Return(int id)
    {
        var loan = await _db.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .Include(l => l.LoanedByUser)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound(new { error = "Loan not found." });
        if (loan.ReturnedAt.HasValue) return BadRequest(new { error = "Loan already returned." });

        loan.ReturnedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToDto(loan));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var loan = await _db.Loans.FindAsync(id);
        if (loan == null) return NotFound(new { error = "Loan not found." });
        if (!loan.ReturnedAt.HasValue) return StatusCode(403, new { error = "Can only delete returned loans." });

        _db.Loans.Remove(loan);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Loan deleted." });
    }

    private static LoanResponseDto ToDto(Loan l) => new(
        l.Id, l.BookId, l.Book.Title, l.MemberId, l.Member.FullName,
        l.LoanedAt, l.DueDate, l.ReturnedAt, l.Status, l.LoanedByUser.Username
    );
}
