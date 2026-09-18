namespace CampusPulse.Api.DTOs;

public class ReportCreateDto
{
    // Exactly one of these should be set - reporting a post or a comment.
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
    // No UserId - derived server-side from the caller's JWT.

    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
}

public class ReportReviewDto
{
    // Reviewed / ActionTaken / Dismissed
    public string Status { get; set; } = null!;
}
