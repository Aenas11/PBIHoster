using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IGroupRepository _groupRepo;

    public AdminController(IUserRepository userRepo, IGroupRepository groupRepo)
    {
        _userRepo = userRepo;
        _groupRepo = groupRepo;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? term)
    {
        var results = await _userRepo.SearchAsync(term ?? string.Empty);
        return Ok(results);
    }

    public class UpsertUserDto
    {
        public string Username { get; set; } = string.Empty;
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
        if (dto.Groups != null) user.Groups = dto.Groups;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _userRepo.UpsertAsync(user);
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
        await _userRepo.DeleteAsync(username);
        return NoContent();
    }

    [HttpDelete("groups/{id:int}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        await _groupRepo.DeleteAsync(id);
        return NoContent();
    }
}
