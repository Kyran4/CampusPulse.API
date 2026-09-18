namespace CampusPulse.Api.DTOs;

public class ReactionCreateDto
{
    public int PostId { get; set; }
    // No UserId - derived server-side from the caller's JWT.
    public string Type { get; set; } = null!; // Like / Helpful / Interested
}
