namespace CampusPulse.Api.DTOs;

public class RegisterRequest
{
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}
