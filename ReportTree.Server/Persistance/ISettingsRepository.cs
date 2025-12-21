using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface ISettingsRepository
{
    Task<AppSetting?> GetByKeyAsync(string key);
    Task<IEnumerable<AppSetting>> GetAllAsync();
    Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category);
    Task UpsertAsync(AppSetting setting);
    Task DeleteAsync(string key);
}
