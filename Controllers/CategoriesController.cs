using Microsoft.AspNetCore.Authorization;
using CampusPulse.Api.Data;
using CampusPulse.Api.DTOs;
using CampusPulse.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<Category>>> GetCategories()
    {
        return await _db.Categories.ToListAsync();
    }

    // Only Admins add/edit/remove categories (Admin story under Events).
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Category>> Create(CategoryCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        if (await _db.Categories.AnyAsync(c => c.Name == dto.Name))
            return BadRequest("A category with this name already exists.");

        var category = new Category { Name = dto.Name, Icon = dto.Icon };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        if (await _db.Posts.AnyAsync(p => p.CategoryId == id) || await _db.Events.AnyAsync(e => e.CategoryId == id))
            return BadRequest("Category is in use and cannot be deleted.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
