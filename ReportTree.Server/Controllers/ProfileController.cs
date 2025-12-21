using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.DTOs;
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

    public ProfileController(AuthService authService, IGroupRepository groupRepo, AuditLogService auditLogService)
    {
        _authService = authService;
        _groupRepo = groupRepo;
        _auditLogService = auditLogService;
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
}
