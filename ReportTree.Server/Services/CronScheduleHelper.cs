using NCrontab;

namespace ReportTree.Server.Services;

public static class CronScheduleHelper
{
    public static bool TryGetNextOccurrence(
        string cron,
        string timeZoneId,
        DateTime fromUtc,
        out DateTime nextUtc,
        out string? error)
    {
        nextUtc = DateTime.MinValue;
        error = null;

        try
        {
            var schedule = CrontabSchedule.Parse(cron, new CrontabSchedule.ParseOptions
            {
                IncludingSeconds = false
            });

            var timeZone = ResolveTimeZone(timeZoneId);
            var localFrom = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, timeZone);
            var nextLocal = schedule.GetNextOccurrence(localFrom);
            nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocal, timeZone);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryValidate(string cron, string timeZoneId, out string? error)
    {
        return TryGetNextOccurrence(cron, timeZoneId, DateTime.UtcNow, out _, out error);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
}
