namespace CampusPulse.Api.DTOs;

public class EventCreateDto
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Location { get; set; } = null!;
    public string? ImageBase64 { get; set; }
    public int CategoryId { get; set; }
    public int? Capacity { get; set; }
}
