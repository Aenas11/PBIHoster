using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfSettingsRepository : ISettingsRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfSettingsRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<AppSetting?> GetByKeyAsync(string key)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Settings.FirstOrDefaultAsync(x => x.Key == key);
    }

    public async Task<IEnumerable<AppSetting>> GetAllAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Settings.ToListAsync();
    }

    public async Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Settings.Where(x => x.Category == category).ToListAsync();
    }

    public async Task UpsertAsync(AppSetting setting)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var existing = await dbContext.Settings.FirstOrDefaultAsync(x => x.Key == setting.Key);
        setting.LastModified = DateTime.UtcNow;

        if (existing == null)
        {
            dbContext.Settings.Add(setting);
        }
        else
        {
            setting.Id = existing.Id;
            dbContext.Entry(existing).CurrentValues.SetValues(setting);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(string key)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var rows = await dbContext.Settings.Where(x => x.Key == key).ToListAsync();
        if (rows.Count == 0)
        {
            return;
        }

        dbContext.Settings.RemoveRange(rows);
        await dbContext.SaveChangesAsync();
    }
}