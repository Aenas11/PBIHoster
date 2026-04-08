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
        return GetAllAsync(new AuditLogQuery
        {
            Skip = skip,
            Take = take,
            ActionType = actionType
        });
    }

    public Task<IEnumerable<AuditLog>> GetAllAsync(AuditLogQuery query)
    {
        var normalized = Normalize(query);
        var liteQuery = ApplyFilters(_collection.Query(), normalized);

        var logs = liteQuery
            .OrderByDescending(x => x.Timestamp)
            .Skip(normalized.Skip)
            .Limit(normalized.Take)
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
        return GetCountAsync(new AuditLogQuery
        {
            ActionType = actionType
        });
    }

    public Task<long> GetCountAsync(AuditLogQuery query)
    {
        var normalized = Normalize(query);
        var count = ApplyFilters(_collection.Query(), normalized)
            .Count();

        return Task.FromResult((long)count);
    }

    private static ILiteQueryable<AuditLog> ApplyFilters(ILiteQueryable<AuditLog> query, AuditLogQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.ActionType))
        {
            query = query.Where(x => x.Action == filters.ActionType);
        }

        if (!string.IsNullOrWhiteSpace(filters.Username))
        {
            query = query.Where(x => x.Username == filters.Username);
        }

        if (!string.IsNullOrWhiteSpace(filters.Resource))
        {
            query = query.Where(x => x.Resource == filters.Resource);
        }

        if (filters.FromUtc.HasValue)
        {
            query = query.Where(x => x.Timestamp >= filters.FromUtc.Value);
        }

        if (filters.ToUtc.HasValue)
        {
            query = query.Where(x => x.Timestamp <= filters.ToUtc.Value);
        }

        if (filters.Success.HasValue)
        {
            query = query.Where(x => x.Success == filters.Success.Value);
        }

        return query;
    }

    private static AuditLogQuery Normalize(AuditLogQuery query)
    {
        return new AuditLogQuery
        {
            Skip = Math.Max(query.Skip, 0),
            Take = query.Take <= 0 ? 100 : query.Take,
            Username = string.IsNullOrWhiteSpace(query.Username) ? null : query.Username,
            ActionType = string.IsNullOrWhiteSpace(query.ActionType) ? null : query.ActionType,
            Resource = string.IsNullOrWhiteSpace(query.Resource) ? null : query.Resource,
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,
            Success = query.Success
        };
    }
}
