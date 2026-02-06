using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface IDatasetRefreshRunRepository
{
    Task AddAsync(DatasetRefreshRun run);
    Task UpdateAsync(DatasetRefreshRun run);
    Task<DatasetRefreshRun?> GetByIdAsync(Guid id);
    Task<DatasetRefreshRun?> GetLatestByDatasetIdAsync(string datasetId);
    Task<DatasetRefreshRun?> GetLatestByScheduleIdAsync(Guid scheduleId);
    Task<IEnumerable<DatasetRefreshRun>> GetByDatasetIdAsync(string datasetId, int skip = 0, int take = 50);
    Task<IEnumerable<DatasetRefreshRun>> GetActiveAsync();
    Task<IEnumerable<DatasetRefreshRun>> GetFailedRunsAsync(int take = 100);
}
