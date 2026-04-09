using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfUserRepository : IUserRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfUserRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task UpsertAsync(AppUser user)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var existing = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == user.Username);
        if (existing == null)
        {
            dbContext.Users.Add(user);
        }
        else
        {
            user.Id = existing.Id;
            dbContext.Entry(existing).CurrentValues.SetValues(user);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Users.FirstOrDefaultAsync(x => x.Username == username);
    }

    public async Task<IEnumerable<AppUser>> SearchAsync(string term)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(term))
        {
            return await dbContext.Users.ToListAsync();
        }

        return await dbContext.Users.Where(x => x.Username.Contains(term)).ToListAsync();
    }

    public async Task DeleteAsync(string username)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == username);
        if (user == null)
        {
            return;
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> CountAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Users.CountAsync();
    }
}