namespace CampusPulse.Api.Models;

public class Post
{
    public int PostId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageBase64 { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsHidden { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public List<Comment> Comments { get; set; } = new();
    public List<Reaction> Reactions { get; set; } = new();
    public List<Report> Reports { get; set; } = new();
}
