using bibliotekarnia.Api.Dtos;
using bibliotekarnia.Data;
using bibliotekarnia.Models;
using bibliotekarnia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Api;

[ApiController]
[Route("api/books")]
public class BooksApiController : ControllerBase
{
    private readonly LibraryDbContext _db;
    private readonly LoanService _loanService;

    public BooksApiController(LibraryDbContext db, LoanService loanService)
    {
        _db = db;
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var books = await _db.Books.Include(b => b.Author).ToListAsync();
        var activeLoans = await _db.Loans
            .Where(l => l.ReturnedAt == null)
            .GroupBy(l => l.BookId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var result = books.Select(b => new BookResponseDto(
            b.Id, b.Title, b.ISBN, b.PublishedYear, b.Genre, b.TotalCopies,
            Math.Max(0, b.TotalCopies - activeLoans.GetValueOrDefault(b.Id, 0)),
            b.AuthorId, b.Author.FullName
        )).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var book = await _db.Books.Include(b => b.Author).FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound(new { error = "Book not found." });

        var available = await _loanService.GetAvailableCopiesAsync(id);
        return Ok(new BookResponseDto(book.Id, book.Title, book.ISBN, book.PublishedYear, book.Genre,
            book.TotalCopies, available, book.AuthorId, book.Author.FullName));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var authorExists = await _db.Authors.AnyAsync(a => a.Id == dto.AuthorId);
        if (!authorExists) return BadRequest(new { error = "Author not found." });

        var book = new Book
        {
            Title = dto.Title,
            ISBN = dto.ISBN,
            PublishedYear = dto.PublishedYear,
            Genre = dto.Genre,
            TotalCopies = dto.TotalCopies,
            AuthorId = dto.AuthorId
        };
        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        await _db.Entry(book).Reference(b => b.Author).LoadAsync();
        var result = new BookResponseDto(book.Id, book.Title, book.ISBN, book.PublishedYear, book.Genre,
            book.TotalCopies, book.TotalCopies, book.AuthorId, book.Author.FullName);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var book = await _db.Books.Include(b => b.Author).FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound(new { error = "Book not found." });

        var authorExists = await _db.Authors.AnyAsync(a => a.Id == dto.AuthorId);
        if (!authorExists) return BadRequest(new { error = "Author not found." });

        var activeLoans = await _db.Loans.CountAsync(l => l.BookId == id && l.ReturnedAt == null);
        if (dto.TotalCopies < activeLoans)
            return BadRequest(new { error = $"Cannot set copies below active loans count ({activeLoans})." });

        book.Title = dto.Title;
        book.ISBN = dto.ISBN;
        book.PublishedYear = dto.PublishedYear;
        book.Genre = dto.Genre;
        book.TotalCopies = dto.TotalCopies;
        book.AuthorId = dto.AuthorId;
        await _db.SaveChangesAsync();

        await _db.Entry(book).Reference(b => b.Author).LoadAsync();
        var available = await _loanService.GetAvailableCopiesAsync(id);
        return Ok(new BookResponseDto(book.Id, book.Title, book.ISBN, book.PublishedYear, book.Genre,
            book.TotalCopies, available, book.AuthorId, book.Author.FullName));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var activeLoans = await _db.Loans.AnyAsync(l => l.BookId == id && l.ReturnedAt == null);
        if (activeLoans) return StatusCode(403, new { error = "Cannot delete book with active loans." });

        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound(new { error = "Book not found." });

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Book deleted." });
    }
}
