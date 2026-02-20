using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using Microsoft.AspNetCore.Http;

namespace ReportTree.Server.Services;

public class AuditLogService
{
    private readonly IAuditLogRepository _repo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(IAuditLogRepository repo, IHttpContextAccessor httpContextAccessor)
    {
        _repo = repo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string resource, string details = "", bool success = true)
    {
        var context = _httpContextAccessor.HttpContext;
        var username = context?.User?.Identity?.Name ?? "Anonymous";
        var ipAddress = context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = context?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";

        var log = new AuditLog
        {
            Username = username,
            Action = action,
            Resource = resource,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            Timestamp = DateTime.UtcNow
        };

        await _repo.AddAsync(log);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(int skip = 0, int take = 100, string? actionType = null)
    {
        return await _repo.GetAllAsync(skip, take, actionType);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByUsernameAsync(string username, int skip = 0, int take = 100)
    {
        return await _repo.GetByUsernameAsync(username, skip, take);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByResourceAsync(string resource, int skip = 0, int take = 100)
    {
        return await _repo.GetByResourceAsync(resource, skip, take);
    }

    public async Task<long> GetCountAsync(string? actionType = null)
    {
        return await _repo.GetCountAsync(actionType);
    }
}
