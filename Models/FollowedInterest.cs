namespace CampusPulse.Api.Models;

// The User -> Followed Interest/Category relationship. Deliberately its own
// join table (not a direct many-to-many navigation) so it can carry its own
// key, timestamp, and a clean unique constraint - see AppDbContext for the
// (UserId, CategoryId) uniqueness rule that prevents duplicate follows.
public class FollowedInterest
{
    public int FollowedInterestId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
