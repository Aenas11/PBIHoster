using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfAuditLogRepository : IAuditLogRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfAuditLogRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(AuditLog log)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        log.Timestamp = DateTime.UtcNow;
        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync();
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

    public async Task<IEnumerable<AuditLog>> GetAllAsync(AuditLogQuery query)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var normalized = Normalize(query);

        return await ApplyFilters(dbContext.AuditLogs.AsQueryable(), normalized)
            .OrderByDescending(x => x.Timestamp)
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUsernameAsync(string username, int skip = 0, int take = 100)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.AuditLogs
            .Where(x => x.Username == username)
            .OrderByDescending(x => x.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByResourceAsync(string resource, int skip = 0, int take = 100)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.AuditLogs
            .Where(x => x.Resource == resource)
            .OrderByDescending(x => x.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public Task<long> GetCountAsync(string? actionType = null)
    {
        return GetCountAsync(new AuditLogQuery
        {
            ActionType = actionType
        });
    }

    public async Task<long> GetCountAsync(AuditLogQuery query)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var normalized = Normalize(query);
        return await ApplyFilters(dbContext.AuditLogs.AsQueryable(), normalized).LongCountAsync();
    }

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogQuery filters)
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