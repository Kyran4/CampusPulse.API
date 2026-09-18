using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Models;

namespace CampusPulse.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FollowedInterest> FollowedInterests => Set<FollowedInterest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Uniqueness / required-field rules the risk register calls for ----
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        // One reaction per user per post (US-13: "cannot spam the same
        // reaction on a post repeatedly" - enforced as one reaction total,
        // changing type replaces it rather than stacking).
        modelBuilder.Entity<Reaction>()
            .HasIndex(r => new { r.PostId, r.UserId })
            .IsUnique();

        // Can't register twice for the same event (US-16).
        modelBuilder.Entity<EventRegistration>()
            .HasIndex(er => new { er.EventId, er.UserId })
            .IsUnique();

        // Can't follow the same interest/category twice (change request:
        // "prevent unnecessary duplicate follow records").
        modelBuilder.Entity<FollowedInterest>()
            .HasIndex(f => new { f.UserId, f.CategoryId })
            .IsUnique();

        // ---- User relationships ----
        modelBuilder.Entity<User>()
            .HasMany(u => u.Posts)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Comments)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Reactions)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Report.UserId = the reporter. Restrict (not cascade) so deleting a
        // user doesn't wipe out reports made against other people's content.
        modelBuilder.Entity<User>()
            .HasMany(u => u.Reports)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.EventRegistrations)
            .WithOne(er => er.User)
            .HasForeignKey(er => er.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user's own followed interests - deleting the user cleans these
        // up too. Deleting a Category while users follow it is blocked
        // (Restrict) the same way it's already blocked for Posts/Events -
        // see CategoriesController.Delete.
        modelBuilder.Entity<FollowedInterest>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FollowedInterest>()
            .HasOne(f => f.Category)
            .WithMany()
            .HasForeignKey(f => f.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment moderation - who (if anyone) hid it. Never cascade-delete
        // a user just because they once moderated a comment.
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ModeratedBy)
            .WithMany()
            .HasForeignKey(c => c.ModeratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Report review - which Admin reviewed it.
        modelBuilder.Entity<Report>()
            .HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Report -> Comment (nullable side; a report targets a Post OR a Comment)
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Comment)
            .WithMany()
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.Post)
            .WithMany(p => p.Reports)
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Category relationships ----
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Posts)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasMany(c => c.Events)
            .WithOne(e => e.Category)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Post relationships ----
        modelBuilder.Entity<Post>()
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Post>()
            .HasMany(p => p.Reactions)
            .WithOne(r => r.Post)
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Event relationships ----
        modelBuilder.Entity<Event>()
            .HasMany(e => e.Registrations)
            .WithOne(er => er.Event)
            .HasForeignKey(er => er.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
