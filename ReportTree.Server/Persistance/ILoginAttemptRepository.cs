using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public interface ILoginAttemptRepository
{
    Task AddAttemptAsync(LoginAttempt attempt);
    Task<IEnumerable<LoginAttempt>> GetRecentAttemptsAsync(string username, TimeSpan period);
    Task<AccountLockout?> GetLockoutAsync(string username);
    Task UpsertLockoutAsync(AccountLockout lockout);
    Task RemoveLockoutAsync(string username);
}
