namespace ReportTree.Server.Services;

public class RefreshOptions
{
    public int MaxConcurrentPerDataset { get; set; } = 1;
    public int ManualCooldownSeconds { get; set; } = 60;
    public int DefaultRetryCount { get; set; } = 2;
    public int DefaultRetryBackoffSeconds { get; set; } = 120;
    public int PollIntervalSeconds { get; set; } = 60;
}
