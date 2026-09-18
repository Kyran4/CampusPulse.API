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
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{postId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments(int postId)
    {
        return await _db.Comments
            .Where(c => c.PostId == postId && !c.IsHidden)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Comment>> Create(CommentCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Comment cannot be empty.");

        if (!await _db.Posts.AnyAsync(p => p.PostId == dto.PostId && !p.IsHidden))
            return NotFound("Post does not exist.");

        var comment = new Comment
        {
            PostId = dto.PostId,
            UserId = User.GetUserId(),
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return await _db.Comments.Include(c => c.User).FirstAsync(c => c.CommentId == comment.CommentId);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Comment>> Update(int id, CommentUpdateDto dto)
    {
        var comment = await _db.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        // Same ownership rule as posts (US-12).
        if (comment.UserId != User.GetUserId() && !User.IsAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Comment cannot be empty.");

        comment.Content = dto.Content;
        await _db.SaveChangesAsync();
        return comment;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var comment = await _db.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        if (comment.UserId != User.GetUserId() && !User.IsAdmin())
            return Forbid();

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Admin moderation - hide with a logged reason (US-12: "Admin moderation
    // is logged with a reason").
    [HttpPut("{id}/hide")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Hide(int id, ModerationReasonDto dto)
    {
        var comment = await _db.Comments.FindAsync(id);
        if (comment == null) return NotFound();

        comment.IsHidden = true;
        comment.ModeratedByUserId = User.GetUserId();
        comment.ModerationReason = dto.Reason;

        await _db.SaveChangesAsync();
        return Ok();
    }
}
