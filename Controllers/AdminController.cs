using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.DTOs;
using CampusPulse.Api.Helpers;

namespace CampusPulse.Api.Controllers;

// Everything here requires Admin - checked server-side regardless of what
// the client's menu shows (Section 6 risk register: "Authorization only
// exists in the UI").
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboard()
    {
        return new DashboardStatsDto
        {
            TotalUsers = await _db.Users.CountAsync(),
            ActiveUsers = await _db.Users.CountAsync(u => u.IsActive),
            PostCount = await _db.Posts.CountAsync(p => !p.IsHidden),
            EventCount = await _db.Events.CountAsync(e => !e.IsCancelled),
            PendingReports = await _db.Reports.CountAsync(r => r.Status == "Pending")
        };
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserAdminDto>>> GetUsers([FromQuery] string? search)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.DisplayName.Contains(search) || u.Email.Contains(search));
        }

        return await query
            .Select(u => new UserAdminDto
            {
                UserId = u.UserId,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .ToListAsync();
    }

    [HttpPut("users/{id}/deactivate")]
    public async Task<ActionResult> Deactivate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // An admin can't lock themselves out via this endpoint by accident-
        // still allowed if they really mean to, but worth being deliberate
        // about; left permissive here since the brief doesn't require a
        // self-lock guard, only that a normal user can't self-promote.
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("users/{id}/reactivate")]
    public async Task<ActionResult> Reactivate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = true;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
