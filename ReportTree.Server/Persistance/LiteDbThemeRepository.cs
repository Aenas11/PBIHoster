using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbThemeRepository : IThemeRepository
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<CustomTheme> _themes;

    public LiteDbThemeRepository(LiteDatabase db)
    {
        _db = db;
        _themes = _db.GetCollection<CustomTheme>("themes");
        
        // Create indexes
        _themes.EnsureIndex(x => x.OrganizationId);
        _themes.EnsureIndex(x => x.CreatedBy);
    }

    public Task<CustomTheme?> GetByIdAsync(string id)
    {
        var theme = _themes.FindById(id);
        return Task.FromResult<CustomTheme?>(theme);
    }

    public Task<IEnumerable<CustomTheme>> GetAllAsync()
    {
        var themes = _themes.FindAll();
        return Task.FromResult(themes);
    }

    public Task<IEnumerable<CustomTheme>> GetByOrganizationAsync(string organizationId)
    {
        var themes = _themes.Find(x => x.OrganizationId == organizationId);
        return Task.FromResult(themes);
    }

    public Task<CustomTheme> CreateAsync(CustomTheme theme)
    {
        theme.Id = Guid.NewGuid().ToString();
        theme.CreatedAt = DateTime.UtcNow;
        _themes.Insert(theme);
        return Task.FromResult(theme);
    }

    public Task<CustomTheme?> UpdateAsync(string id, CustomTheme theme)
    {
        var existing = _themes.FindById(id);
        if (existing == null)
            return Task.FromResult<CustomTheme?>(null);

        theme.Id = id;
        theme.CreatedAt = existing.CreatedAt;
        theme.CreatedBy = existing.CreatedBy;
        theme.UpdatedAt = DateTime.UtcNow;
        
        _themes.Update(theme);
        return Task.FromResult<CustomTheme?>(theme);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var result = _themes.Delete(id);
        return Task.FromResult(result);
    }
}
