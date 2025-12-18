using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Security;

namespace ReportTree.Server.Services;

public class AuthService
{
    private readonly IUserRepository _repo;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository repo, ITokenService tokenService)
    {
        _repo = repo;
        _tokenService = tokenService;
    }

    public async Task RegisterAsync(string username, string password, List<string> roles)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new AppUser 
        { 
            Username = username, 
            PasswordHash = passwordHash, 
            Roles = roles,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.UpsertAsync(user);
    }

    public async Task<string?> LoginAsync(string username, string password)
    {
        var user = await _repo.GetByUsernameAsync(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }
        
        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _repo.UpsertAsync(user);
        
        return _tokenService.Generate(user);
    }

    public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        var user = await _repo.GetByUsernameAsync(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _repo.UpsertAsync(user);
        return true;
    }

    public async Task<bool> UpdateProfileAsync(string username, string email)
    {
        var user = await _repo.GetByUsernameAsync(username);
        if (user == null)
        {
            return false;
        }

        user.Email = email;
        await _repo.UpsertAsync(user);
        return true;
    }

    public async Task<AppUser?> GetUserAsync(string username)
    {
        return await _repo.GetByUsernameAsync(username);
    }
}
