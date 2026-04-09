using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfGroupRepository : IGroupRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfGroupRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<Group>> GetAllAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Groups.ToListAsync();
    }

    public async Task<IEnumerable<Group>> SearchAsync(string term)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(term))
        {
            return await dbContext.Groups.ToListAsync();
        }

        return await dbContext.Groups.Where(x => x.Name.Contains(term)).ToListAsync();
    }

    public async Task<int> CreateAsync(Group group)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        return group.Id;
    }

    public async Task DeleteAsync(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == id);
        if (group == null)
        {
            return;
        }

        dbContext.Groups.Remove(group);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Group group)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Groups.Update(group);
        await dbContext.SaveChangesAsync();
    }
}