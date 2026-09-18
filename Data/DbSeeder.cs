using CampusPulse.Api.Data;
using CampusPulse.Api.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // ---------------------------------------------------------
        // 1. Seed Categories FIRST (required for Posts/Events)
        // ---------------------------------------------------------
        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "General", Icon = "general.png" },
                new Category { Name = "Events", Icon = "events.png" },
                new Category { Name = "Announcements", Icon = "announce.png" }
            );

            await db.SaveChangesAsync();
        }

        var generalCategory = await db.Categories.FirstAsync(c => c.Name == "General");
        var eventsCategory = await db.Categories.FirstAsync(c => c.Name == "Events");

        // ---------------------------------------------------------
        // 2. Seed Admin User
        // ---------------------------------------------------------
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@campuspulse.com");

        if (admin == null)
        {
            admin = new User
            {
                DisplayName = "Admin",
                Email = "admin@campuspulse.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin",
                IsActive = true,
                ProfileImageUrl = null
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
        else if (admin.Role != "Admin" || !admin.IsActive)
        {
            // Self-healing: this account was created by an earlier version
            // of this seeder (back when Role defaulted to lowercase
            // "admin"). Because seeding only runs the "create" branch above
            // when the row doesn't exist yet, that stale value has survived
            // every schema migration since - the column was always there,
            // just holding the wrong string. [Authorize(Roles = "Admin")]
            // does an exact, case-sensitive match, so "admin" != "Admin"
            // and every admin-only endpoint correctly 403'd a technically-
            // valid, technically-authenticated token. Fixing it up here
            // means any future schema/seed change can't reintroduce this.
            admin.Role = "Admin";
            admin.IsActive = true;
            await db.SaveChangesAsync();
        }

        // ---------------------------------------------------------
        // 3. Seed Posts (must reference valid Category + User)
        // ---------------------------------------------------------
        if (!db.Posts.Any())
        {
            db.Posts.AddRange(
                new Post
                {
                    Title = "Welcome to CampusPulse!",
                    Content = "This is the first post on the platform.",
                    ImageBase64 = "https://picsum.photos/600/400?random=1",
                    UserId = admin.UserId,
                    CategoryId = generalCategory.CategoryId
                },
                new Post
                {
                    Title = "Campus BBQ",
                    Content = "Free food this Friday at the quad!",
                    ImageBase64 = "https://picsum.photos/600/400?random=2",
                    UserId = admin.UserId,
                    CategoryId = generalCategory.CategoryId
                },
                new Post
                {
                    Title = "Study Group Tonight",
                    Content = "Join us in the library at 6pm.",
                    ImageBase64 = "https://picsum.photos/600/400?random=3",
                    UserId = admin.UserId,
                    CategoryId = generalCategory.CategoryId
                }
            );

            await db.SaveChangesAsync();
        }

        // ---------------------------------------------------------
        // 4. Seed Events (needs Category + CreatedBy now)
        // ---------------------------------------------------------
        if (!db.Events.Any())
        {
            db.Events.AddRange(
                new Event
                {
                    Title = "Coding Workshop",
                    Description = "Learn C# basics with hands-on examples.",
                    Date = DateTime.UtcNow.AddDays(3),
                    Location = "Tech Building Room 204",
                    ImageBase64 = "https://picsum.photos/600/400?random=4",
                    CategoryId = eventsCategory.CategoryId,
                    Capacity = 30,
                    CreatedByUserId = admin.UserId
                },
                new Event
                {
                    Title = "Campus Party",
                    Description = "DJ + Pizza + Games!",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "Main Quad",
                    ImageBase64 = "https://picsum.photos/600/400?random=5",
                    CategoryId = eventsCategory.CategoryId,
                    Capacity = null,
                    CreatedByUserId = admin.UserId
                }
            );

            await db.SaveChangesAsync();
        }
    }

    // ---------------------------------------------------------
    // OPTIONAL: Manual wipe + reseed (dev only)
    // ---------------------------------------------------------
    public static async Task ResetAndReseedAsync(AppDbContext db)
    {
        db.Comments.RemoveRange(db.Comments);
        db.Reactions.RemoveRange(db.Reactions);
        db.EventRegistrations.RemoveRange(db.EventRegistrations);
        db.Posts.RemoveRange(db.Posts);
        db.Events.RemoveRange(db.Events);
        db.Categories.RemoveRange(db.Categories);
        db.Users.RemoveRange(db.Users);

        await db.SaveChangesAsync();
        await SeedAsync(db);
    }
}
