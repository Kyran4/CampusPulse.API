namespace CampusPulse.Api.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Icon { get; set; }

    // Navigation
    public List<Post> Posts { get; set; } = new();
    public List<Event> Events { get; set; } = new();
}
