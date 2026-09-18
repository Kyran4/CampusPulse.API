namespace CampusPulse.Api.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;

        // "Student" or "Admin". Never settable by the user themselves -
        // only ever assigned server-side (registration = Student, or by
        // another Admin via the admin user-management endpoint).
        public string Role { get; set; } = "Student";

        public bool IsActive { get; set; } = true;
        public string? ProfileImageUrl { get; set; }

        public string PasswordHash { get; set; } = null!;

        // Navigation
        public List<Post> Posts { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public List<Reaction> Reactions { get; set; } = new();
        public List<Report> Reports { get; set; } = new();
        public List<EventRegistration> EventRegistrations { get; set; } = new();
    }
}
