using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbDatasetRefreshRunRepository : IDatasetRefreshRunRepository
{
    private readonly ILiteCollection<DatasetRefreshRun> _collection;

    public LiteDbDatasetRefreshRunRepository(LiteDatabase db)
    {
        _collection = db.GetCollection<DatasetRefreshRun>("dataset_refresh_runs");
        _collection.EnsureIndex(x => x.DatasetId);
        _collection.EnsureIndex(x => x.ScheduleId);
        _collection.EnsureIndex(x => x.Status);
        _collection.EnsureIndex(x => x.RequestedAtUtc);
    }

    public Task AddAsync(DatasetRefreshRun run)
    {
        _collection.Insert(run);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DatasetRefreshRun run)
    {
        _collection.Update(run);
        return Task.CompletedTask;
    }

    public Task<DatasetRefreshRun?> GetByIdAsync(Guid id)
    {
        return Task.FromResult<DatasetRefreshRun?>(_collection.FindById(id));
    }

    public Task<DatasetRefreshRun?> GetLatestByDatasetIdAsync(string datasetId)
    {
        var run = _collection.Query()
            .Where(x => x.DatasetId == datasetId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .FirstOrDefault();
        return Task.FromResult<DatasetRefreshRun?>(run);
    }

    public Task<DatasetRefreshRun?> GetLatestByScheduleIdAsync(Guid scheduleId)
    {
        var run = _collection.Query()
            .Where(x => x.ScheduleId == scheduleId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .FirstOrDefault();
        return Task.FromResult<DatasetRefreshRun?>(run);
    }

    public Task<IEnumerable<DatasetRefreshRun>> GetByDatasetIdAsync(string datasetId, int skip = 0, int take = 50)
    {
        var runs = _collection.Query()
            .Where(x => x.DatasetId == datasetId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .Skip(skip)
            .Limit(take)
            .ToEnumerable();
        return Task.FromResult(runs);
    }

    public Task<IEnumerable<DatasetRefreshRun>> GetActiveAsync()
    {
        var runs = _collection.Query()
            .Where(x => x.Status == RefreshStatus.Queued || x.Status == RefreshStatus.InProgress)
            .OrderBy(x => x.RequestedAtUtc)
            .ToEnumerable();
        return Task.FromResult(runs);
    }

    public Task<IEnumerable<DatasetRefreshRun>> GetFailedRunsAsync(int take = 100)
    {
        var runs = _collection.Query()
            .Where(x => x.Status == RefreshStatus.Failed && x.ScheduleId != null)
            .OrderByDescending(x => x.CompletedAtUtc)
            .Limit(take)
            .ToEnumerable();
        return Task.FromResult(runs);
    }
}
