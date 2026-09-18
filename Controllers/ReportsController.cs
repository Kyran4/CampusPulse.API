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
public class ReportsController : ControllerBase
{
    // "Pending" is included so a report can be reverted back to needing
    // review (undo) - not just moved forward through Reviewed/ActionTaken/
    // Dismissed.
    private static readonly string[] ValidStatuses = { "Pending", "Reviewed", "ActionTaken", "Dismissed" };

    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    // Admin-only: reviewing reports is a moderation action.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Report>>> GetReports([FromQuery] string? status)
    {
        var query = _db.Reports.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        return await query
            .Include(r => r.User)
            .Include(r => r.Post)
            .Include(r => r.Comment)
            .Include(r => r.ReviewedBy)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Report>> Create(ReportCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest("A reason is required.");

        // Exactly one target - a report is against a post OR a comment, never both/neither.
        var targetCount = (dto.PostId.HasValue ? 1 : 0) + (dto.CommentId.HasValue ? 1 : 0);
        if (targetCount != 1)
            return BadRequest("A report must target exactly one post or comment.");

        if (dto.PostId.HasValue && !await _db.Posts.AnyAsync(p => p.PostId == dto.PostId))
            return NotFound("Post does not exist.");

        if (dto.CommentId.HasValue && !await _db.Comments.AnyAsync(c => c.CommentId == dto.CommentId))
            return NotFound("Comment does not exist.");

        var report = new Report
        {
            PostId = dto.PostId,
            CommentId = dto.CommentId,
            UserId = User.GetUserId(),
            Reason = dto.Reason,
            Description = dto.Description,
            Status = "Pending", // never anything else at creation - reporting never deletes content directly
            CreatedAt = DateTime.UtcNow
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        return report;
    }

    // Admin marks it Reviewed / ActionTaken / Dismissed and it's recorded
    // which Admin did it.
    [HttpPut("{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Report>> Review(int id, ReportReviewDto dto)
    {
        if (!ValidStatuses.Contains(dto.Status))
            return BadRequest($"Status must be one of: {string.Join(", ", ValidStatuses)}");

        var report = await _db.Reports.FindAsync(id);
        if (report == null) return NotFound();

        report.Status = dto.Status;

        if (dto.Status == "Pending")
        {
            // Reverting to Pending means "this needs review again" - clear
            // who/when it was reviewed rather than leaving a stale record
            // that implies it's still been actioned.
            report.ReviewedByUserId = null;
            report.ReviewedAt = null;
        }
        else
        {
            report.ReviewedByUserId = User.GetUserId();
            report.ReviewedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return report;
    }
}
