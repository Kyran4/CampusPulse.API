namespace CampusPulse.Api.DTOs;

public class EventJoinDto
{
    public int EventId { get; set; }
    // No UserId - derived server-side from the caller's JWT.
}
