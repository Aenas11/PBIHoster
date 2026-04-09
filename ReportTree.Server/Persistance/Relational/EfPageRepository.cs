using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfPageRepository : IPageRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfPageRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<Page>> GetAllAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Pages.ToListAsync();
    }

    public async Task<Page?> GetByIdAsync(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Pages.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<int> CreateAsync(Page page)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Pages.Add(page);
        await dbContext.SaveChangesAsync();
        return page.Id;
    }

    public async Task UpdateAsync(Page page)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Pages.Update(page);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var page = await dbContext.Pages.FirstOrDefaultAsync(x => x.Id == id);
        if (page == null)
        {
            return;
        }

        dbContext.Pages.Remove(page);
        await dbContext.SaveChangesAsync();
    }
}