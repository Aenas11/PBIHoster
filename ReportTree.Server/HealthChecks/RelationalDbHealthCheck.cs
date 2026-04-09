using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Persistance.Relational;

namespace ReportTree.Server.HealthChecks;

public class RelationalDbHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public RelationalDbHealthCheck(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Relational database is reachable.")
                : HealthCheckResult.Unhealthy("Cannot connect to relational database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Relational database health check failed.", ex);
        }
    }
}