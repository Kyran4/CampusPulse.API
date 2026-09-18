using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.DTOs;
using CampusPulse.Api.Helpers;

namespace CampusPulse.Api.Controllers;

// Self-service profile management - every action here operates on the
// caller's own account (from the JWT), never a UserId in the request. This
// is deliberately separate from AdminController.Deactivate/Reactivate,
// which is the Admin-only path for acting on OTHER users' accounts. A
// Student can edit their own DisplayName/Email/avatar here, but this
// endpoint has no Role field at all - there's no way to reach self-promotion
// through it, by construction rather than by a check that could be missed.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user == null) return NotFound();

        return new UserDto
        {
            UserId = user.UserId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            ProfileImageUrl = user.ProfileImageUrl
        };
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName) || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest("Display name and email are required.");

        if (!IsValidEmail(dto.Email))
            return BadRequest("Please enter a valid email address.");

        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != userId);
        if (emailTaken)
            return BadRequest("That email is already in use.");

        user.DisplayName = dto.DisplayName;
        user.Email = dto.Email;
        user.ProfileImageUrl = dto.ProfileImageUrl;

        await _db.SaveChangesAsync();

        return new UserDto
        {
            UserId = user.UserId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            ProfileImageUrl = user.ProfileImageUrl
        };
    }

    private static bool IsValidEmail(string email)
    {
        try { return new System.Net.Mail.MailAddress(email).Address == email; }
        catch { return false; }
    }
}
