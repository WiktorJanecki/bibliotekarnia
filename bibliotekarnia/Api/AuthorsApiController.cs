using bibliotekarnia.Api.Dtos;
using bibliotekarnia.Data;
using bibliotekarnia.Models;
using bibliotekarnia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Api;

[ApiController]
[Route("api/authors")]
public class AuthorsApiController : ControllerBase
{
    private readonly LibraryDbContext _db;
    private readonly LoanService _loanService;

    public AuthorsApiController(LibraryDbContext db, LoanService loanService)
    {
        _db = db;
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var authors = await _db.Authors
            .Include(a => a.Books)
            .OrderBy(a => a.LastName)
            .Select(a => new AuthorResponseDto(a.Id, a.FirstName, a.LastName, a.BirthYear, a.Nationality, a.Books.Count))
            .ToListAsync();
        return Ok(authors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var author = await _db.Authors.Include(a => a.Books).FirstOrDefaultAsync(a => a.Id == id);
        if (author == null) return NotFound(new { error = "Author not found." });

        var activeLoansMap = await _db.Loans
            .Where(l => l.ReturnedAt == null && author.Books.Select(b => b.Id).Contains(l.BookId))
            .GroupBy(l => l.BookId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var bookDtos = author.Books.Select(b => new BookSummaryDto(
            b.Id, b.Title, b.Genre, b.PublishedYear, b.TotalCopies,
            Math.Max(0, b.TotalCopies - activeLoansMap.GetValueOrDefault(b.Id, 0))
        )).ToList();

        return Ok(new AuthorDetailDto(author.Id, author.FirstName, author.LastName, author.BirthYear, author.Nationality, bookDtos));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AuthorRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var author = new Author
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            BirthYear = dto.BirthYear,
            Nationality = dto.Nationality
        };
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();

        var result = new AuthorResponseDto(author.Id, author.FirstName, author.LastName, author.BirthYear, author.Nationality, 0);
        return CreatedAtAction(nameof(GetById), new { id = author.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AuthorRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var author = await _db.Authors.FindAsync(id);
        if (author == null) return NotFound(new { error = "Author not found." });

        author.FirstName = dto.FirstName;
        author.LastName = dto.LastName;
        author.BirthYear = dto.BirthYear;
        author.Nationality = dto.Nationality;
        await _db.SaveChangesAsync();

        return Ok(new AuthorResponseDto(author.Id, author.FirstName, author.LastName, author.BirthYear, author.Nationality,
            await _db.Books.CountAsync(b => b.AuthorId == id)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var author = await _db.Authors.Include(a => a.Books).FirstOrDefaultAsync(a => a.Id == id);
        if (author == null) return NotFound(new { error = "Author not found." });
        if (author.Books.Any()) return StatusCode(403, new { error = "Cannot delete author with books." });

        _db.Authors.Remove(author);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Author deleted." });
    }
}
