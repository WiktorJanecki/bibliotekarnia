using bibliotekarnia.Api.Dtos;
using bibliotekarnia.Data;
using bibliotekarnia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Api;

[ApiController]
[Route("api/members")]
public class MembersApiController : ControllerBase
{
    private readonly LibraryDbContext _db;

    public MembersApiController(LibraryDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var members = await _db.Members.Include(m => m.Loans).OrderBy(m => m.LastName).ToListAsync();
        var result = members.Select(m => new MemberResponseDto(
            m.Id, m.FirstName, m.LastName, m.Email, m.Phone, m.MemberSince,
            m.Loans.Count(l => l.ReturnedAt == null),
            m.Loans.Count
        )).ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var member = await _db.Members.Include(m => m.Loans).FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return NotFound(new { error = "Member not found." });

        return Ok(new MemberResponseDto(
            member.Id, member.FirstName, member.LastName, member.Email, member.Phone, member.MemberSince,
            member.Loans.Count(l => l.ReturnedAt == null),
            member.Loans.Count
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MemberRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var exists = await _db.Members.AnyAsync(m => m.Email == dto.Email);
        if (exists) return BadRequest(new { error = "A member with this email already exists." });

        var member = new Member
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            MemberSince = dto.MemberSince.ToUniversalTime()
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = member.Id },
            new MemberResponseDto(member.Id, member.FirstName, member.LastName, member.Email, member.Phone, member.MemberSince, 0, 0));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] MemberRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var member = await _db.Members.FindAsync(id);
        if (member == null) return NotFound(new { error = "Member not found." });

        var exists = await _db.Members.AnyAsync(m => m.Email == dto.Email && m.Id != id);
        if (exists) return BadRequest(new { error = "Email already used by another member." });

        member.FirstName = dto.FirstName;
        member.LastName = dto.LastName;
        member.Email = dto.Email;
        member.Phone = dto.Phone;
        member.MemberSince = dto.MemberSince.ToUniversalTime();
        await _db.SaveChangesAsync();

        var totalLoans = await _db.Loans.CountAsync(l => l.MemberId == id);
        var activeLoans = await _db.Loans.CountAsync(l => l.MemberId == id && l.ReturnedAt == null);
        return Ok(new MemberResponseDto(member.Id, member.FirstName, member.LastName, member.Email, member.Phone,
            member.MemberSince, activeLoans, totalLoans));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var hasActiveLoans = await _db.Loans.AnyAsync(l => l.MemberId == id && l.ReturnedAt == null);
        if (hasActiveLoans) return StatusCode(403, new { error = "Cannot delete member with active loans." });

        var member = await _db.Members.FindAsync(id);
        if (member == null) return NotFound(new { error = "Member not found." });

        var relatedLoans = await _db.Loans.Where(l => l.MemberId == id).ToListAsync();
        _db.Loans.RemoveRange(relatedLoans);

        _db.Members.Remove(member);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Member deleted." });
    }
}
