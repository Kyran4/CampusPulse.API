using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.DTOs;
using CampusPulse.Api.Models;
using CampusPulse.Api.Helpers;

namespace CampusPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        return await _db.Events
            .Include(e => e.Category)
            .OrderBy(e => e.Date)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Event>> GetEvent(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Category)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null) return NotFound();
        return ev;
    }

    [HttpGet("{id}/attendees")]
    public async Task<ActionResult<IEnumerable<User>>> GetAttendees(int id)
    {
        return await _db.EventRegistrations
            .Where(er => er.EventId == id)
            .Include(er => er.User)
            .Select(er => er.User)
            .ToListAsync();
    }

    [HttpPost("join")]
    public async Task<ActionResult> Join(EventJoinDto dto)
    {
        var ev = await _db.Events.FindAsync(dto.EventId);
        if (ev == null) return NotFound("Event does not exist.");

        var userId = User.GetUserId();

        // US-16 acceptance criteria, all enforced server-side:
        if (ev.IsCancelled)
            return BadRequest("This event has been cancelled.");

        if (ev.Date < DateTime.UtcNow)
            return BadRequest("This event has already happened.");

        if (await _db.EventRegistrations.AnyAsync(er => er.EventId == dto.EventId && er.UserId == userId))
            return BadRequest("You are already registered for this event.");

        if (ev.Capacity.HasValue)
        {
            var currentCount = await _db.EventRegistrations.CountAsync(er => er.EventId == dto.EventId);
            if (currentCount >= ev.Capacity.Value)
                return BadRequest("This event is full.");
        }

        _db.EventRegistrations.Add(new EventRegistration
        {
            EventId = dto.EventId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok();
    }

    // US-17: can only cancel your own registration.
    [HttpPost("leave")]
    public async Task<ActionResult> Leave(EventJoinDto dto)
    {
        var userId = User.GetUserId();

        var reg = await _db.EventRegistrations
            .FirstOrDefaultAsync(er => er.EventId == dto.EventId && er.UserId == userId);

        if (reg == null) return NotFound("You are not registered for this event.");

        _db.EventRegistrations.Remove(reg);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // ---- Admin: create/manage events ----

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Event>> Create(EventCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.Location))
            return BadRequest("Title, description and location are required.");

        if (dto.Date <= DateTime.UtcNow)
            return BadRequest("Event date cannot be in the past.");

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
            return BadRequest("Category does not exist.");

        if (dto.Capacity.HasValue && dto.Capacity.Value <= 0)
            return BadRequest("Capacity must be a positive number.");

        var ev = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            Date = dto.Date,
            Location = dto.Location,
            ImageBase64 = dto.ImageBase64,
            CategoryId = dto.CategoryId,
            Capacity = dto.Capacity,
            CreatedByUserId = User.GetUserId(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return ev;
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Event>> Update(int id, EventCreateDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
            return BadRequest("Category does not exist.");

        ev.Title = dto.Title;
        ev.Description = dto.Description;
        ev.Date = dto.Date;
        ev.Location = dto.Location;
        ev.ImageBase64 = dto.ImageBase64;
        ev.CategoryId = dto.CategoryId;
        ev.Capacity = dto.Capacity;

        await _db.SaveChangesAsync();
        return ev;
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Cancel(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();

        ev.IsCancelled = true;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
