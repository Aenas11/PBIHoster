using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using System.Security.Claims;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThemesController : ControllerBase
{
    private readonly IThemeRepository _themeRepository;

    public ThemesController(IThemeRepository themeRepository)
    {
        _themeRepository = themeRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ThemeDto>>> GetThemes([FromQuery] string? organizationId)
    {
        IEnumerable<CustomTheme> themes;
        
        if (!string.IsNullOrEmpty(organizationId))
        {
            themes = await _themeRepository.GetByOrganizationAsync(organizationId);
        }
        else
        {
            themes = await _themeRepository.GetAllAsync();
        }

        return Ok(themes.Select(t => t.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ThemeDto>> GetTheme(string id)
    {
        var theme = await _themeRepository.GetByIdAsync(id);
        if (theme == null)
            return NotFound();

        return Ok(theme.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<ThemeDto>> CreateTheme([FromBody] CreateThemeDto dto)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        
        var theme = new CustomTheme
        {
            Name = dto.Name,
            Tokens = dto.Tokens,
            OrganizationId = dto.OrganizationId,
            CreatedBy = username,
            IsCustom = true
        };

        var created = await _themeRepository.CreateAsync(theme);
        return CreatedAtAction(nameof(GetTheme), new { id = created.Id }, created.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<ThemeDto>> UpdateTheme(string id, [FromBody] UpdateThemeDto dto)
    {
        var existing = await _themeRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        // Check if user has permission to update
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var isAdmin = User.IsInRole("Admin");
        
        if (!isAdmin && existing.CreatedBy != username)
            return Forbid();

        existing.Name = dto.Name;
        existing.Tokens = dto.Tokens;

        var updated = await _themeRepository.UpdateAsync(id, existing);
        return Ok(updated?.ToDto());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult> DeleteTheme(string id)
    {
        var existing = await _themeRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        // Check if user has permission to delete
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var isAdmin = User.IsInRole("Admin");
        
        if (!isAdmin && existing.CreatedBy != username)
            return Forbid();

        var result = await _themeRepository.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
