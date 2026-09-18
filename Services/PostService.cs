using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.Models;

namespace CampusPulse.Api.Services;

public class PostService
{
    private readonly AppDbContext _db;

    public PostService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Post>> GetFeedAsync()
    {
        return await _db.Posts
            .Where(p => !p.IsHidden)
            .Include(p => p.User)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Post>> GetUserPostsAsync(int userId)
    {
        return await _db.Posts
            .Where(p => p.UserId == userId && !p.IsHidden)
            .Include(p => p.User)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Post?> GetPostAsync(int id)
    {
        return await _db.Posts
            .Include(p => p.User)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.PostId == id);
    }
}
