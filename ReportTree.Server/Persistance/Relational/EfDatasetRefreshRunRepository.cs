using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfDatasetRefreshRunRepository : IDatasetRefreshRunRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfDatasetRefreshRunRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(DatasetRefreshRun run)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.RefreshRuns.Add(run);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(DatasetRefreshRun run)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.RefreshRuns.Update(run);
        await dbContext.SaveChangesAsync();
    }

    public async Task<DatasetRefreshRun?> GetByIdAsync(Guid id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.RefreshRuns.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DatasetRefreshRun?> GetLatestByDatasetIdAsync(string datasetId)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.RefreshRuns
            .Where(x => x.DatasetId == datasetId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<DatasetRefreshRun?> GetLatestByScheduleIdAsync(Guid scheduleId)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.RefreshRuns
            .Where(x => x.ScheduleId == scheduleId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DatasetRefreshRun>> GetByDatasetIdAsync(string datasetId, int skip = 0, int take = 50)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.RefreshRuns
            .Where(x => x.DatasetId == datasetId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<DatasetRefreshRun>> GetActiveAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.RefreshRuns
            .Where(x => x.Status == RefreshStatus.Queued || x.Status == RefreshStatus.InProgress)
            .OrderBy(x => x.RequestedAtUtc)
            .ToListAsync();
    }

    public async Task<IEnumerable<DatasetRefreshRun>> GetFailedRunsAsync(int take = 100)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.RefreshRuns
            .Where(x => x.Status == RefreshStatus.Failed && x.ScheduleId != null)
            .OrderByDescending(x => x.CompletedAtUtc)
            .Take(take)
            .ToListAsync();
    }
}