using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class EfLoginAttemptRepository : ILoginAttemptRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public EfLoginAttemptRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAttemptAsync(LoginAttempt attempt)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        attempt.AttemptTime = DateTime.UtcNow;
        dbContext.LoginAttempts.Add(attempt);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<LoginAttempt>> GetRecentAttemptsAsync(string username, TimeSpan period)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var threshold = DateTime.UtcNow - period;
        return await dbContext.LoginAttempts
            .Where(x => x.Username == username && x.AttemptTime > threshold)
            .OrderByDescending(x => x.AttemptTime)
            .ToListAsync();
    }

    public async Task<AccountLockout?> GetLockoutAsync(string username)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.AccountLockouts.FirstOrDefaultAsync(x => x.Username == username);
    }

    public async Task UpsertLockoutAsync(AccountLockout lockout)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var existing = await dbContext.AccountLockouts.FirstOrDefaultAsync(x => x.Username == lockout.Username);
        if (existing == null)
        {
            dbContext.AccountLockouts.Add(lockout);
        }
        else
        {
            lockout.Id = existing.Id;
            dbContext.Entry(existing).CurrentValues.SetValues(lockout);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveLockoutAsync(string username)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var existing = await dbContext.AccountLockouts.Where(x => x.Username == username).ToListAsync();
        if (existing.Count == 0)
        {
            return;
        }

        dbContext.AccountLockouts.RemoveRange(existing);
        await dbContext.SaveChangesAsync();
    }
}