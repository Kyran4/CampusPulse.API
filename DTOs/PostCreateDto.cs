namespace CampusPulse.Api.DTOs;

public class PostCreateDto
{
    // No UserId here on purpose - the server derives the owner from the
    // caller's JWT (see PostsController.Create), never from client input.
    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;

    public string? ImageBase64 { get; set; }
}
