using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<IEnumerable<AuditLog>> GetAllAsync(int skip = 0, int take = 100);
    Task<IEnumerable<AuditLog>> GetByUsernameAsync(string username, int skip = 0, int take = 100);
    Task<IEnumerable<AuditLog>> GetByResourceAsync(string resource, int skip = 0, int take = 100);
    Task<long> GetCountAsync();
}
