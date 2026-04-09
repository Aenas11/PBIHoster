using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfThemeRepository : IThemeRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfThemeRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CustomTheme?> GetByIdAsync(string id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Themes.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<CustomTheme>> GetAllAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Themes.ToListAsync();
    }

    public async Task<IEnumerable<CustomTheme>> GetByOrganizationAsync(string organizationId)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Themes.Where(x => x.OrganizationId == organizationId).ToListAsync();
    }

    public async Task<CustomTheme> CreateAsync(CustomTheme theme)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        theme.Id = Guid.NewGuid().ToString();
        theme.CreatedAt = DateTime.UtcNow;
        dbContext.Themes.Add(theme);
        await dbContext.SaveChangesAsync();
        return theme;
    }

    public async Task<CustomTheme?> UpdateAsync(string id, CustomTheme theme)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var existing = await dbContext.Themes.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null)
        {
            return null;
        }

        var createdAt = existing.CreatedAt;
        var createdBy = existing.CreatedBy;

        dbContext.Entry(existing).CurrentValues.SetValues(theme);
        existing.Id = id;
        existing.CreatedAt = createdAt;
        existing.CreatedBy = createdBy;
        existing.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var existing = await dbContext.Themes.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null)
        {
            return false;
        }

        dbContext.Themes.Remove(existing);
        await dbContext.SaveChangesAsync();
        return true;
    }
}