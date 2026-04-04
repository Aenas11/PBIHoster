using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface IUsageEventRepository
{
    Task AddRangeAsync(IEnumerable<UsageEvent> events);
    Task<IEnumerable<UsageEvent>> GetRangeAsync(DateTime fromUtc, DateTime toUtc);
}