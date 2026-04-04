using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbUsageEventRepository : IUsageEventRepository
{
    private readonly ILiteCollection<UsageEvent> _collection;

    public LiteDbUsageEventRepository(LiteDatabase db)
    {
        _collection = db.GetCollection<UsageEvent>("usage_events");
        _collection.EnsureIndex(x => x.Timestamp);
        _collection.EnsureIndex(x => x.EventType);
        _collection.EnsureIndex(x => x.Username);
        _collection.EnsureIndex(x => x.Path);
    }

    public Task AddRangeAsync(IEnumerable<UsageEvent> events)
    {
        var batch = events.Select(e =>
        {
            e.Timestamp = DateTime.UtcNow;
            return e;
        }).ToList();

        if (batch.Count > 0)
        {
            _collection.InsertBulk(batch);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<UsageEvent>> GetRangeAsync(DateTime fromUtc, DateTime toUtc)
    {
        var results = _collection
            .Query()
            .Where(x => x.Timestamp >= fromUtc && x.Timestamp <= toUtc)
            .ToEnumerable();

        return Task.FromResult(results);
    }
}