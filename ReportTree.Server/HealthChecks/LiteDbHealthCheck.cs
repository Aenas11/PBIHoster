using LiteDB;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ReportTree.Server.HealthChecks;

public class LiteDbHealthCheck : IHealthCheck
{
    private readonly LiteDatabase _database;

    public LiteDbHealthCheck(LiteDatabase database)
    {
        _database = database;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = _database.GetCollectionNames();
            return Task.FromResult(HealthCheckResult.Healthy("LiteDB is reachable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("LiteDB check failed.", ex));
        }
    }
}
