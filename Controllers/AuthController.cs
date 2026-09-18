using CampusPulse.Api.Data;
using CampusPulse.Api.DTOs;
using CampusPulse.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest("Display name, email and password are all required.");
        }

        if (!IsValidEmail(dto.Email))
            return BadRequest("Please enter a valid email address.");

        if (!IsValidPassword(dto.Password))
            return BadRequest("Password must be at least 8 characters and include an uppercase letter, a lowercase letter, a digit and a symbol.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

        var user = new User
        {
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            // Always Student at registration - role escalation only happens
            // through the Admin user-management endpoint, never here.
            Role = "Student",
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            ProfileImageUrl = null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await Login(new LoginRequest
        {
            Email = dto.Email,
            Password = dto.Password
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequest dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // Deliberately the same generic message whether the email doesn't
        // exist, the password is wrong, or the account is deactivated -
        // US-02: "does not say which one was wrong".
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash) || !user.IsActive)
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            User = new UserDto
            {
                UserId = user.UserId,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                ProfileImageUrl = user.ProfileImageUrl
            },
            Token = token
        };
    }

    private static bool IsValidEmail(string email)
    {
        try { return new System.Net.Mail.MailAddress(email).Address == email; }
        catch { return false; }
    }

    private static bool IsValidPassword(string password)
    {
        if (password.Length < 8) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(ch => "!@#$%^&*()_+-=[]{}|;:'\",.<>/?".Contains(ch))) return false;
        return true;
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim("role", user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
