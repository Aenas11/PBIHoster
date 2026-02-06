using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbDatasetRefreshScheduleRepository : IDatasetRefreshScheduleRepository
{
    private readonly ILiteCollection<DatasetRefreshSchedule> _collection;

    public LiteDbDatasetRefreshScheduleRepository(LiteDatabase db)
    {
        _collection = db.GetCollection<DatasetRefreshSchedule>("dataset_refresh_schedules");
        _collection.EnsureIndex(x => x.WorkspaceId);
        _collection.EnsureIndex(x => x.DatasetId);
        _collection.EnsureIndex(x => x.Enabled);
    }

    public Task<IEnumerable<DatasetRefreshSchedule>> GetAllAsync()
    {
        return Task.FromResult(_collection.FindAll());
    }

    public Task<DatasetRefreshSchedule?> GetByIdAsync(Guid id)
    {
        return Task.FromResult<DatasetRefreshSchedule?>(_collection.FindById(id));
    }

    public Task CreateAsync(DatasetRefreshSchedule schedule)
    {
        _collection.Insert(schedule);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DatasetRefreshSchedule schedule)
    {
        _collection.Update(schedule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
