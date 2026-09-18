using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.Models;

namespace CampusPulse.Api.Services;

public class EventService
{
    private readonly AppDbContext _db;

    public EventService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Event>> GetEventsAsync()
    {
        return await _db.Events
            .OrderBy(e => e.Date)
            .ToListAsync();
    }

    public async Task<List<User>> GetAttendeesAsync(int eventId)
    {
        return await _db.EventRegistrations
            .Where(er => er.EventId == eventId)
            .Include(er => er.User)
            .Select(er => er.User)
            .ToListAsync();
    }
}
