using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbAuditLogRepository : IAuditLogRepository
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<AuditLog> _collection;

    public LiteDbAuditLogRepository(LiteDatabase db)
    {
        _db = db;
        _collection = _db.GetCollection<AuditLog>("auditlogs");
        _collection.EnsureIndex(x => x.Username);
        _collection.EnsureIndex(x => x.Resource);
        _collection.EnsureIndex(x => x.Action);
        _collection.EnsureIndex(x => x.Timestamp);
    }

    public Task AddAsync(AuditLog log)
    {
        log.Timestamp = DateTime.UtcNow;
        _collection.Insert(log);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditLog>> GetAllAsync(int skip = 0, int take = 100, string? actionType = null)
    {
        var query = _collection.Query();
        
        if (!string.IsNullOrEmpty(actionType))
        {
            query = query.Where(x => x.Action == actionType);
        }
        
        var logs = query
            .OrderByDescending(x => x.Timestamp)
            .Skip(skip)
            .Limit(take)
            .ToEnumerable();
        return Task.FromResult(logs);
    }

    public Task<IEnumerable<AuditLog>> GetByUsernameAsync(string username, int skip = 0, int take = 100)
    {
        var logs = _collection
            .Query()
            .Where(x => x.Username == username)
            .OrderByDescending(x => x.Timestamp)
            .Skip(skip)
            .Limit(take)
            .ToEnumerable();
        return Task.FromResult(logs);
    }

    public Task<IEnumerable<AuditLog>> GetByResourceAsync(string resource, int skip = 0, int take = 100)
    {
        var logs = _collection
            .Query()
            .Where(x => x.Resource == resource)
            .OrderByDescending(x => x.Timestamp)
            .Skip(skip)
            .Limit(take)
            .ToEnumerable();
        return Task.FromResult(logs);
    }

    public Task<long> GetCountAsync(string? actionType = null)
    {
        if (string.IsNullOrEmpty(actionType))
        {
            return Task.FromResult((long)_collection.Count());
        }
        
        var count = _collection.Query()
            .Where(x => x.Action == actionType)
            .Count();
        return Task.FromResult((long)count);
    }
}
