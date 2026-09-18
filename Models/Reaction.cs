namespace CampusPulse.Api.Models;

public class Reaction
{
    public int ReactionId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }

    public string Type { get; set; } = null!; // like, love, etc.

    // Navigation
    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
