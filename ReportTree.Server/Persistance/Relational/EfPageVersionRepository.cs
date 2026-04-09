using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfPageVersionRepository : IPageVersionRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfPageVersionRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<int> CreateAsync(PageVersion version)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        version.ChangedAt = DateTime.UtcNow;
        dbContext.PageVersions.Add(version);
        await dbContext.SaveChangesAsync();
        return version.Id;
    }

    public async Task<IEnumerable<PageVersion>> GetByPageIdAsync(int pageId, int take = 50)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.PageVersions
            .Where(x => x.PageId == pageId)
            .OrderByDescending(x => x.ChangedAt)
            .Take(Math.Max(1, take))
            .ToListAsync();
    }

    public async Task<PageVersion?> GetByIdAsync(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.PageVersions.FirstOrDefaultAsync(x => x.Id == id);
    }
}