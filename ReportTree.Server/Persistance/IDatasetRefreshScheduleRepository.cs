using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface IDatasetRefreshScheduleRepository
{
    Task<IEnumerable<DatasetRefreshSchedule>> GetAllAsync();
    Task<DatasetRefreshSchedule?> GetByIdAsync(Guid id);
    Task CreateAsync(DatasetRefreshSchedule schedule);
    Task UpdateAsync(DatasetRefreshSchedule schedule);
    Task DeleteAsync(Guid id);
}
