namespace CampusPulse.Api.DTOs;

// What the Admin user-management screen shows per user - no password hash.
public class UserAdminDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int PostCount { get; set; }
    public int EventCount { get; set; }
    public int PendingReports { get; set; }
}
