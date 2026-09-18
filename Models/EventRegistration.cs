namespace CampusPulse.Api.Models;

public class EventRegistration
{
    public int EventRegistrationId { get; set; }
    public int EventId { get; set; }
    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
    public User User { get; set; } = null!;
}
