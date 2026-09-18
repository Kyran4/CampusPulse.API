namespace CampusPulse.Api.DTOs;

public class UpdateProfileDto
{
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    // Base64-encoded image data or a URL - same convention as Post/Event
    // images. Null/empty clears the avatar.
    public string? ProfileImageUrl { get; set; }
}
