using Microsoft.EntityFrameworkCore;
using CampusPulse.Api.Data;
using CampusPulse.Api.Models;

namespace CampusPulse.Api.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserAsync(int id)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
    }
}
