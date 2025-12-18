namespace ReportTree.Server.Models;

public class LoginAttempt
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime AttemptTime { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}

public class AccountLockout
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime LockedUntil { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime LastAttempt { get; set; } = DateTime.UtcNow;
}
