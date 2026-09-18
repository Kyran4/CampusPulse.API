namespace CampusPulse.Api.DTOs;

public class AuthResponseDto
{
    public UserDto User { get; set; } = null!;
    public string Token { get; set; } = null!;
}

// What the client ever sees about a user - deliberately excludes
// PasswordHash. The previous version of this DTO wrapped the raw User
// entity, which meant every login/register response shipped the caller's
// bcrypt hash to the client for no reason.
public class UserDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? ProfileImageUrl { get; set; }
}
