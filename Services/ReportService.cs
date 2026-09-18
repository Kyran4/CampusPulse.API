using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.Models;

namespace CampusPulse.Api.Services;

public class ReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Report>> GetReportsAsync()
    {
        return await _db.Reports
            .Include(r => r.User)
            .Include(r => r.Post)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}
