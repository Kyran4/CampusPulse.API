namespace CampusPulse.Api.Models;

public class Event
{
    public int EventId { get; set; }

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Location { get; set; } = null!;
    public string? ImageBase64 { get; set; }

    public int CategoryId { get; set; }
    public int? Capacity { get; set; } // null = unlimited
    public int CreatedByUserId { get; set; } // Admin who created it
    public bool IsCancelled { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Category Category { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public List<EventRegistration> Registrations { get; set; } = new();
}
