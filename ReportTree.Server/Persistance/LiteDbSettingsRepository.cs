using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbSettingsRepository : ISettingsRepository
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<AppSetting> _collection;

    public LiteDbSettingsRepository(LiteDatabase db)
    {
        _db = db;
        _collection = _db.GetCollection<AppSetting>("settings");
        _collection.EnsureIndex(x => x.Key, true);
    }

    public Task<AppSetting?> GetByKeyAsync(string key)
    {
        var setting = _collection.FindOne(x => x.Key == key);
        return Task.FromResult<AppSetting?>(setting);
    }

    public Task<IEnumerable<AppSetting>> GetAllAsync()
    {
        var settings = _collection.FindAll();
        return Task.FromResult(settings);
    }

    public Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category)
    {
        var settings = _collection.Find(x => x.Category == category);
        return Task.FromResult(settings);
    }

    public Task UpsertAsync(AppSetting setting)
    {
        setting.LastModified = DateTime.UtcNow;
        var existing = _collection.FindOne(x => x.Key == setting.Key);
        if (existing != null)
        {
            setting.Id = existing.Id;
        }
        _collection.Upsert(setting);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        _collection.DeleteMany(x => x.Key == key);
        return Task.CompletedTask;
    }
}
