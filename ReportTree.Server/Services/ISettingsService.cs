using ReportTree.Server.Models;

namespace ReportTree.Server.Services
{
    public interface ISettingsService
    {
        Task<AppSetting?> GetSettingAsync(string key);
        Task<string?> GetValueAsync(string key);
        Task<bool> IsDemoModeEnabledAsync();
        Task<IEnumerable<AppSetting>> GetAllSettingsAsync();
        Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category);
        Task UpsertSettingAsync(string key, string value, string category, string description, bool isEncrypted, string modifiedBy);
        Task DeleteSettingAsync(string key);
        Task InitializeDefaultSettingsAsync();
    }
}
