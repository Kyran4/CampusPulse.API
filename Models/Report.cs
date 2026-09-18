namespace CampusPulse.Api.Models;

public class Report
{
    public int ReportId { get; set; }

    // Exactly one of these is set: a report targets a post OR a comment.
    public int? PostId { get; set; }
    public int? CommentId { get; set; }

    public int UserId { get; set; } // ReportedByUserId

    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Pending / Reviewed / ActionTaken / Dismissed
    public string Status { get; set; } = "Pending";
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Navigation
    public Post? Post { get; set; }
    public Comment? Comment { get; set; }
    public User User { get; set; } = null!;
    public User? ReviewedBy { get; set; }
}
