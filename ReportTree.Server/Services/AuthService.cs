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
            Roles = roles 
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
        return _tokenService.Generate(user);
    }
}
