namespace CampusPulse.Api.DTOs;

public class FollowCategoryDto
{
    public int CategoryId { get; set; }
    // No UserId - the API derives the follower from the caller's JWT, same
    // pattern as every other create-style DTO in this project.
}
