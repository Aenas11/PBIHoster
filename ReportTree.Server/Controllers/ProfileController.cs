using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Services;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IGroupRepository _groupRepo;
    private readonly AuditLogService _auditLogService;
    private readonly IPageRepository _pageRepo;
    private readonly PageAuthorizationService _pageAuthorization;
    private readonly SettingsService _settingsService;
    private readonly DemoContentService _demoContentService;
    private const int MaxRecentPages = 10;

    public ProfileController(
        AuthService authService,
        IGroupRepository groupRepo,
        AuditLogService auditLogService,
        IPageRepository pageRepo,
        PageAuthorizationService pageAuthorization,
        SettingsService settingsService,
        DemoContentService demoContentService)
    {
        _authService = authService;
        _groupRepo = groupRepo;
        _auditLogService = auditLogService;
        _pageRepo = pageRepo;
        _pageAuthorization = pageAuthorization;
        _settingsService = settingsService;
        _demoContentService = demoContentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserAsync(username);
        if (user == null)
        {
            return NotFound();
        }

        // Get user's groups
        var allGroups = await _groupRepo.GetAllAsync();
        var userGroups = allGroups
            .Where(g => g.Members.Contains(username))
            .Select(g => g.Name)
            .ToList();

        var profile = new UserProfileDto(
            user.Username,
            user.Email,
            user.Roles,
            userGroups
        );

        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var success = await _authService.UpdateProfileAsync(username, request.Email);
        if (!success)
        {
            return NotFound();
        }

        await _auditLogService.LogAsync("UPDATE", $"Profile:{username}", "Updated profile information");
        return Ok();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var (success, errors) = await _authService.ChangePasswordAsync(username, request.CurrentPassword, request.NewPassword);
        if (!success)
        {
            await _auditLogService.LogAsync("CHANGE_PASSWORD", $"User:{username}", "Failed to change password", false);
            return BadRequest(new { Errors = errors });
        }

        await _auditLogService.LogAsync("CHANGE_PASSWORD", $"User:{username}", "Successfully changed password");
        return Ok(new { message = "Password changed successfully" });
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        user.FavoritePageIds ??= new List<int>();
        return Ok(user.FavoritePageIds);
    }

    [HttpPost("favorites/{pageId:int}")]
    public async Task<IActionResult> AddFavorite(int pageId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        user.FavoritePageIds ??= new List<int>();

        var page = await GetAccessiblePageAsync(pageId);
        if (page == null)
        {
            return NotFound();
        }

        if (!user.FavoritePageIds.Contains(pageId))
        {
            user.FavoritePageIds.Add(pageId);
            await _authService.UpdateUserAsync(user);
        }

        return Ok(new { added = true });
    }

    [HttpDelete("favorites/{pageId:int}")]
    public async Task<IActionResult> RemoveFavorite(int pageId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        user.FavoritePageIds ??= new List<int>();

        if (user.FavoritePageIds.Remove(pageId))
        {
            await _authService.UpdateUserAsync(user);
        }

        return Ok(new { removed = true });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        user.RecentPageIds ??= new List<int>();
        return Ok(user.RecentPageIds);
    }

    [HttpPost("recent/{pageId:int}")]
    public async Task<IActionResult> RecordRecent(int pageId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        user.RecentPageIds ??= new List<int>();

        var page = await GetAccessiblePageAsync(pageId);
        if (page == null)
        {
            return NotFound();
        }

        user.RecentPageIds.Remove(pageId);
        user.RecentPageIds.Insert(0, pageId);

        if (user.RecentPageIds.Count > MaxRecentPages)
        {
            user.RecentPageIds = user.RecentPageIds.Take(MaxRecentPages).ToList();
        }

        await _authService.UpdateUserAsync(user);

        return Ok(new { updated = true });
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return null;
        }

        return await _authService.GetUserAsync(username);
    }

    private async Task<Page?> GetAccessiblePageAsync(int pageId)
    {
        var page = await _pageRepo.GetByIdAsync(pageId);
        if (page == null)
        {
            var isDemoMode = await _settingsService.IsDemoModeEnabledAsync();
            if (isDemoMode)
            {
                page = _demoContentService.GetDemoPages().FirstOrDefault(p => p.Id == pageId);
            }
        }

        if (page == null)
        {
            return null;
        }

        return _pageAuthorization.CanAccessPage(page, User) ? page : null;
    }
}
