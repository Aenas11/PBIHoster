using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface IThemeRepository
{
    Task<CustomTheme?> GetByIdAsync(string id);
    Task<IEnumerable<CustomTheme>> GetAllAsync();
    Task<IEnumerable<CustomTheme>> GetByOrganizationAsync(string organizationId);
    Task<CustomTheme> CreateAsync(CustomTheme theme);
    Task<CustomTheme?> UpdateAsync(string id, CustomTheme theme);
    Task<bool> DeleteAsync(string id);
}
