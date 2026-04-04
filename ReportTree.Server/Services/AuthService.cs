using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ReportTree.Server.Services;

public class AuthService
{
    private readonly IUserRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly ILoginAttemptRepository _loginAttemptRepo;
    private readonly PasswordValidator _passwordValidator;
    private readonly PasswordPolicy _passwordPolicy;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ExternalGroupSyncService _externalGroupSyncService;

    public AuthService(
        IUserRepository repo, 
        ITokenService tokenService,
        ILoginAttemptRepository loginAttemptRepo,
        PasswordValidator passwordValidator,
        PasswordPolicy passwordPolicy,
        IHttpContextAccessor httpContextAccessor,
        ExternalGroupSyncService externalGroupSyncService)
    {
        _repo = repo;
        _tokenService = tokenService;
        _loginAttemptRepo = loginAttemptRepo;
        _passwordValidator = passwordValidator;
        _passwordPolicy = passwordPolicy;
        _httpContextAccessor = httpContextAccessor;
        _externalGroupSyncService = externalGroupSyncService;
    }

    public async Task<(bool Success, List<string> Errors)> RegisterAsync(string username, string password, List<string> roles)
    {
        // Validate password
        var (isValid, errors) = _passwordValidator.Validate(password);
        if (!isValid)
        {
            return (false, errors);
        }

        // Check if this is the first user
        var userCount = await _repo.CountAsync();
        if (userCount == 0)
        {
            if (!roles.Contains("Admin"))
            {
                roles.Add("Admin");
            }
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new AppUser 
        { 
            Username = username, 
            PasswordHash = passwordHash, 
            Roles = roles,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.UpsertAsync(user);
        return (true, new List<string>());
    }

    public async Task<(string? Token, string? ErrorMessage)> LoginAsync(string username, string password)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        // Check if account is locked
        var lockout = await _loginAttemptRepo.GetLockoutAsync(username);
        if (lockout != null && lockout.LockedUntil > DateTime.UtcNow)
        {
            await _loginAttemptRepo.AddAttemptAsync(new LoginAttempt
            {
                Username = username,
                IpAddress = ipAddress,
                Success = false,
                FailureReason = "Account locked"
            });
            
            var remainingMinutes = (int)(lockout.LockedUntil - DateTime.UtcNow).TotalMinutes;
            return (null, $"Account is locked. Try again in {remainingMinutes} minutes.");
        }

        var user = await _repo.GetByUsernameAsync(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // Record failed attempt
            await _loginAttemptRepo.AddAttemptAsync(new LoginAttempt
            {
                Username = username,
                IpAddress = ipAddress,
                Success = false,
                FailureReason = "Invalid credentials"
            });

            // Check if we need to lock the account
            var recentAttempts = await _loginAttemptRepo.GetRecentAttemptsAsync(
                username, 
                TimeSpan.FromMinutes(_passwordPolicy.LockoutMinutes)
            );
            var failedCount = recentAttempts.Count(a => !a.Success);

            if (failedCount >= _passwordPolicy.MaxFailedAccessAttempts)
            {
                await _loginAttemptRepo.UpsertLockoutAsync(new AccountLockout
                {
                    Username = username,
                    LockedUntil = DateTime.UtcNow.AddMinutes(_passwordPolicy.LockoutMinutes),
                    FailedAttempts = failedCount,
                    LastAttempt = DateTime.UtcNow
                });
                return (null, $"Too many failed attempts. Account locked for {_passwordPolicy.LockoutMinutes} minutes.");
            }

            return (null, "Invalid username or password");
        }

        // Successful login - record and clear lockout
        await _loginAttemptRepo.AddAttemptAsync(new LoginAttempt
        {
            Username = username,
            IpAddress = ipAddress,
            Success = true
        });
        await _loginAttemptRepo.RemoveLockoutAsync(username);
        
        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _repo.UpsertAsync(user);
        
        return (_tokenService.Generate(user), null);
    }

    public async Task<(string? Token, string? ErrorMessage)> LoginExternalAsync(ExternalAuthProvider provider, ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub")?.Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return (null, "External identity is missing subject claim");
        }

        var normalizedProviderId = provider.Id.Trim().ToLowerInvariant();
        var username = BuildExternalUsername(normalizedProviderId, subject);
        var user = await _repo.GetByUsernameAsync(username);

        if (user == null)
        {
            var email = principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue("email")
                ?? string.Empty;

            user = new AppUser
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                Roles = new List<string> { NormalizeDefaultRole(provider.DefaultRole) },
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };
        }
        else
        {
            var email = principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue("email")
                ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(email))
            {
                user.Email = email;
            }

            if (user.Roles.Count == 0)
            {
                user.Roles.Add("Viewer");
            }

            user.LastLogin = DateTime.UtcNow;
        }

        await _repo.UpsertAsync(user);
        await _externalGroupSyncService.SyncMembershipsAsync(user.Username, provider, principal);
        return (_tokenService.Generate(user), null);
    }

    public async Task<(bool Success, List<string> Errors)> ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        var user = await _repo.GetByUsernameAsync(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return (false, new List<string> { "Current password is incorrect" });
        }

        // Validate new password
        var (isValid, errors) = _passwordValidator.Validate(newPassword);
        if (!isValid)
        {
            return (false, errors);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _repo.UpsertAsync(user);
        return (true, new List<string>());
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

    public async Task UpdateUserAsync(AppUser user)
    {
        await _repo.UpsertAsync(user);
    }

    private static string BuildExternalUsername(string providerId, string subject)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{providerId}:{subject}"));
        var hash = Convert.ToHexString(bytes).ToLowerInvariant()[..20];
        return $"oidc_{providerId}_{hash}";
    }

    private static string NormalizeDefaultRole(string defaultRole)
    {
        if (string.Equals(defaultRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Admin";
        }

        if (string.Equals(defaultRole, "Editor", StringComparison.OrdinalIgnoreCase))
        {
            return "Editor";
        }

        return "Viewer";
    }
}
