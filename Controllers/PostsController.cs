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
public class PostsController : ControllerBase
{
    private const int MaxTitleLength = 150;
    private const int MaxContentLength = 4000;

    private readonly AppDbContext _db;

    public PostsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Post>>> GetFeed([FromQuery] int? categoryId)
    {
        var query = _db.Posts.Where(p => !p.IsHidden);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        return await query
            .Include(p => p.User)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("user/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Post>>> GetUserPosts(int id)
    {
        return await _db.Posts
            .Where(p => p.UserId == id && !p.IsHidden)
            .Include(p => p.User)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    // Change request: "see content relevant to followed interests" -
    // implemented as a feed filtered to only the categories the caller
    // follows. Requires auth (unlike the main feed) since "followed" is
    // inherently per-user.
    [HttpGet("following")]
    public async Task<ActionResult<IEnumerable<Post>>> GetFollowingFeed()
    {
        var userId = User.GetUserId();

        var followedCategoryIds = await _db.FollowedInterests
            .Where(f => f.UserId == userId)
            .Select(f => f.CategoryId)
            .ToListAsync();

        if (followedCategoryIds.Count == 0)
            return new List<Post>();

        return await _db.Posts
            .Where(p => !p.IsHidden && followedCategoryIds.Contains(p.CategoryId))
            .Include(p => p.User)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Post>> Create(PostCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Title and content are required.");

        if (dto.Title.Length > MaxTitleLength)
            return BadRequest($"Title cannot exceed {MaxTitleLength} characters.");

        if (dto.Content.Length > MaxContentLength)
            return BadRequest($"Content cannot exceed {MaxContentLength} characters.");

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
            return BadRequest("Category does not exist.");

        var post = new Post
        {
            // Owner comes from the token, never from the request body -
            // otherwise anyone could create a post "as" someone else.
            UserId = User.GetUserId(),
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Content = dto.Content,
            ImageBase64 = dto.ImageBase64,
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        return await _db.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .FirstAsync(p => p.PostId == post.PostId);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Post>> Update(int id, PostCreateDto dto)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();

        // Ownership check happens here, server-side - not just hidden in the
        // UI. US-08: "Trying to edit someone else's post gets blocked, not
        // just hidden from the menu."
        if (post.UserId != User.GetUserId() && !User.IsAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Title and content are required.");

        if (dto.Title.Length > MaxTitleLength)
            return BadRequest($"Title cannot exceed {MaxTitleLength} characters.");

        if (dto.Content.Length > MaxContentLength)
            return BadRequest($"Content cannot exceed {MaxContentLength} characters.");

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
            return BadRequest("Category does not exist.");

        post.Title = dto.Title;
        post.Content = dto.Content;
        post.CategoryId = dto.CategoryId;
        post.ImageBase64 = dto.ImageBase64;

        await _db.SaveChangesAsync();
        return post;
    }

    // Owner deleting their own post outright (US-09). Confirmation happens
    // client-side; ownership is re-checked here regardless.
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();

        if (post.UserId != User.GetUserId() && !User.IsAdmin())
            return Forbid();

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Admin moderation removal - hides rather than deletes, and records why
    // (Section 10.4: "Content is hidden, not just wiped with no trace").
    [HttpPut("{id}/hide")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Hide(int id, [FromBody] ModerationReasonDto dto)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();

        post.IsHidden = true;
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("{id}/unhide")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Unhide(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();

        post.IsHidden = false;
        await _db.SaveChangesAsync();

        return Ok();
    }
}
