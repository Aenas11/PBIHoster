using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Security;
using System.ComponentModel.DataAnnotations;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IGroupRepository _groupRepo;
    private readonly PasswordValidator _passwordValidator;

    public AdminController(IUserRepository userRepo, IGroupRepository groupRepo, PasswordValidator passwordValidator)
    {
        _userRepo = userRepo;
        _groupRepo = groupRepo;
        _passwordValidator = passwordValidator;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? term)
    {
        var users = await _userRepo.SearchAsync(term ?? string.Empty);
        var groups = (await _groupRepo.GetAllAsync()).ToList();
        
        // Build response with computed groups from Group.Members
        var response = users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Roles,
            Groups = groups.Where(g => g.Members.Contains(u.Username)).Select(g => g.Name).ToList()
        });
        
        return Ok(response);
    }

    public class UpsertUserDto
    {
        [Required, MinLength(3, ErrorMessage = "Username must be at least 3 characters long")]
        public string Username { get; set; } = string.Empty;
        
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string? Password { get; set; }
        
        public List<string>? Roles { get; set; }
        public List<string>? Groups { get; set; }
    }

    [HttpPost("users")]
    public async Task<IActionResult> UpsertUser([FromBody] UpsertUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username)) return BadRequest("Username required");

        var existing = await _userRepo.GetByUsernameAsync(dto.Username);
        var user = existing ?? new AppUser();

        user.Username = dto.Username;
        if (dto.Roles != null) user.Roles = dto.Roles;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            // Validate password policy
            var (isValid, errors) = _passwordValidator.Validate(dto.Password);
            if (!isValid)
            {
                return BadRequest(new { Errors = errors });
            }
            
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _userRepo.UpsertAsync(user);

        // Update group memberships (single source of truth: Group.Members)
        if (dto.Groups != null)
        {
            await UpdateUserGroupMembershipsAsync(user.Username, dto.Groups);
        }

        return Ok();
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups([FromQuery] string? term)
    {
        var results = string.IsNullOrWhiteSpace(term) ? await _groupRepo.GetAllAsync() : await _groupRepo.SearchAsync(term!);
        return Ok(results);
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] Group group)
    {
        if (string.IsNullOrWhiteSpace(group.Name)) return BadRequest("Name required");
        var id = await _groupRepo.CreateAsync(group);
        return CreatedAtAction(nameof(GetGroups), new { id }, group);
    }

    [HttpPut("groups/{id:int}")]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] Group group)
    {
        if (id != group.Id) return BadRequest("ID mismatch");
        await _groupRepo.UpdateAsync(group);
        return NoContent();
    }

    [HttpDelete("users/{username}")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        // Remove user from all groups (single source of truth)
        var allGroups = (await _groupRepo.GetAllAsync()).ToList();
        foreach (var group in allGroups.Where(g => g.Members.Contains(username)))
        {
            group.Members.Remove(username);
            await _groupRepo.UpdateAsync(group);
        }

        await _userRepo.DeleteAsync(username);
        return NoContent();
    }

    [HttpDelete("groups/{id:int}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        // Just delete the group - memberships are stored only in Group.Members
        await _groupRepo.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("groups/{id:int}/members")]
    public async Task<IActionResult> AddGroupMember(int id, [FromBody] AddMemberDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username)) return BadRequest("Username required");

        var group = (await _groupRepo.GetAllAsync()).FirstOrDefault(g => g.Id == id);
        if (group == null) return NotFound("Group not found");

        var user = await _userRepo.GetByUsernameAsync(dto.Username);
        if (user == null) return NotFound("User not found");

        // Add member to group (single source of truth)
        if (!group.Members.Contains(dto.Username))
        {
            group.Members.Add(dto.Username);
            await _groupRepo.UpdateAsync(group);
        }

        return Ok();
    }

    [HttpDelete("groups/{id:int}/members/{username}")]
    public async Task<IActionResult> RemoveGroupMember(int id, string username)
    {
        var group = (await _groupRepo.GetAllAsync()).FirstOrDefault(g => g.Id == id);
        if (group == null) return NotFound("Group not found");

        // Remove member from group (single source of truth)
        group.Members.Remove(username);
        await _groupRepo.UpdateAsync(group);

        return NoContent();
    }

    public class AddMemberDto
    {
        public string Username { get; set; } = string.Empty;
    }

    private async Task UpdateUserGroupMembershipsAsync(string username, List<string> desiredGroups)
    {
        var allGroups = (await _groupRepo.GetAllAsync()).ToList();

        foreach (var group in allGroups)
        {
            bool shouldBeInGroup = desiredGroups.Contains(group.Name);
            bool isInGroup = group.Members.Contains(username);

            if (shouldBeInGroup && !isInGroup)
            {
                // Add user to group
                group.Members.Add(username);
                await _groupRepo.UpdateAsync(group);
            }
            else if (!shouldBeInGroup && isInGroup)
            {
                // Remove user from group
                group.Members.Remove(username);
                await _groupRepo.UpdateAsync(group);
            }
        }
    }

    private List<string> GetUserGroups(string username, IEnumerable<Group> allGroups)
    {
        return allGroups.Where(g => g.Members.Contains(username)).Select(g => g.Name).ToList();
    }
}
