using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfUsageEventRepository : IUsageEventRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfUsageEventRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddRangeAsync(IEnumerable<UsageEvent> events)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            return;
        }

        foreach (var item in eventList)
        {
            item.Timestamp = DateTime.UtcNow;
        }

        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        await dbContext.UsageEvents.AddRangeAsync(eventList);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<UsageEvent>> GetRangeAsync(DateTime fromUtc, DateTime toUtc)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.UsageEvents
            .Where(x => x.Timestamp >= fromUtc && x.Timestamp <= toUtc)
            .ToListAsync();
    }
}