using LiteDB;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class LiteDbLoginAttemptRepository : ILoginAttemptRepository
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<LoginAttempt> _attempts;
    private readonly ILiteCollection<AccountLockout> _lockouts;

    public LiteDbLoginAttemptRepository(LiteDatabase db)
    {
        _db = db;
        _attempts = _db.GetCollection<LoginAttempt>("login_attempts");
        _lockouts = _db.GetCollection<AccountLockout>("account_lockouts");
        
        _attempts.EnsureIndex(x => x.Username);
        _attempts.EnsureIndex(x => x.AttemptTime);
        _lockouts.EnsureIndex(x => x.Username, true);
    }

    public Task AddAttemptAsync(LoginAttempt attempt)
    {
        attempt.AttemptTime = DateTime.UtcNow;
        _attempts.Insert(attempt);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<LoginAttempt>> GetRecentAttemptsAsync(string username, TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        var attempts = _attempts
            .Query()
            .Where(x => x.Username == username && x.AttemptTime > cutoff)
            .OrderByDescending(x => x.AttemptTime)
            .ToEnumerable();
        return Task.FromResult(attempts);
    }

    public Task<AccountLockout?> GetLockoutAsync(string username)
    {
        var lockout = _lockouts.FindOne(x => x.Username == username);
        return Task.FromResult<AccountLockout?>(lockout);
    }

    public Task UpsertLockoutAsync(AccountLockout lockout)
    {
        var existing = _lockouts.FindOne(x => x.Username == lockout.Username);
        if (existing != null)
        {
            lockout.Id = existing.Id;
        }
        _lockouts.Upsert(lockout);
        return Task.CompletedTask;
    }

    public Task RemoveLockoutAsync(string username)
    {
        _lockouts.DeleteMany(x => x.Username == username);
        return Task.CompletedTask;
    }
}
