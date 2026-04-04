using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Services;
using Xunit;

namespace ReportTree.Server.Tests.Security;

public class UsageTrackingServiceTests
{
    [Fact]
    public async Task RecordAsync_AcceptsAllowedEvents_AndCapsBatchSize()
    {
        var repo = new InMemoryUsageEventRepository();
        var service = new UsageTrackingService(repo);

        var events = Enumerable.Range(0, 250)
            .Select(i => new UsageEventRequest("page_view", $"/page/{i}", "desktop", null))
            .ToList();

        var accepted = await service.RecordAsync(events, "alice");

        Assert.Equal(200, accepted);
        Assert.Equal(200, repo.Events.Count);
    }

    [Fact]
    public async Task RecordAsync_IgnoresUnknownEventTypes()
    {
        var repo = new InMemoryUsageEventRepository();
        var service = new UsageTrackingService(repo);

        var accepted = await service.RecordAsync(new[]
        {
            new UsageEventRequest("unknown_type", "/", "desktop", null),
            new UsageEventRequest("page_view", "/home", "desktop", null)
        }, "alice");

        Assert.Equal(1, accepted);
        Assert.Single(repo.Events);
        Assert.Equal("page_view", repo.Events[0].EventType);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsAggregatedCounts()
    {
        var repo = new InMemoryUsageEventRepository();
        var service = new UsageTrackingService(repo);

        await service.RecordAsync(new[]
        {
            new UsageEventRequest("page_view", "/", "desktop", null),
            new UsageEventRequest("page_view", "/", "desktop", null),
            new UsageEventRequest("report_view", "/page/1", "desktop", null)
        }, "alice");

        await service.RecordAsync(new[]
        {
            new UsageEventRequest("page_view", "/page/2", "mobile", null)
        }, "bob");

        var summary = await service.GetSummaryAsync(30);

        Assert.Equal(4, summary.TotalEvents);
        Assert.Equal(2, summary.UniqueUsers);
        Assert.Contains(summary.EventTypes, x => x.EventType == "page_view" && x.Count == 3);
        Assert.Contains(summary.TopPaths, x => x.Path == "/" && x.Count == 2);
    }

    private sealed class InMemoryUsageEventRepository : IUsageEventRepository
    {
        public List<UsageEvent> Events { get; } = new();

        public Task AddRangeAsync(IEnumerable<UsageEvent> events)
        {
            Events.AddRange(events.Select(e =>
            {
                e.Id = Events.Count + 1;
                e.Timestamp = DateTime.UtcNow;
                return e;
            }));

            return Task.CompletedTask;
        }

        public Task<IEnumerable<UsageEvent>> GetRangeAsync(DateTime fromUtc, DateTime toUtc)
        {
            var range = Events.Where(e => e.Timestamp >= fromUtc && e.Timestamp <= toUtc);
            return Task.FromResult<IEnumerable<UsageEvent>>(range);
        }
    }
}
