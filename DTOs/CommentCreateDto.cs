namespace CampusPulse.Api.DTOs;

public class CommentCreateDto
{
    public int PostId { get; set; }
    // No UserId - derived server-side from the caller's JWT.
    public string Content { get; set; } = null!;
}

public class CommentUpdateDto
{
    public string Content { get; set; } = null!;
}

public class ModerationReasonDto
{
    public string? Reason { get; set; }
}
