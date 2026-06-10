using System.Diagnostics;
using System.Text.Json;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Services;

public class UsageTrackingService
{
    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "page_view",
        "report_view",
        "widget_interaction",
        "user_login"
    };

    private readonly IUsageEventRepository _usageEventRepository;
    private readonly MetricsService? _metricsService;

    public UsageTrackingService(IUsageEventRepository usageEventRepository, MetricsService? metricsService = null)
    {
        _usageEventRepository = usageEventRepository;
        _metricsService = metricsService;
    }

    public async Task<int> RecordAsync(IEnumerable<UsageEventRequest> events, string username)
    {
        var stopwatch = Stopwatch.StartNew();

        var incoming = events.Take(200).ToList();
        var normalizedEvents = incoming
            .Where(e => AllowedEventTypes.Contains(e.EventType))
            .Select(e => new UsageEvent
            {
                EventType = e.EventType.Trim().ToLowerInvariant(),
                Username = string.IsNullOrWhiteSpace(username) ? "anonymous" : username,
                Path = NormalizePath(e.Path),
                DeviceType = NormalizeDeviceType(e.DeviceType),
                MetadataJson = SerializeMetadata(e.Metadata)
            })
            .ToList();

        await _usageEventRepository.AddRangeAsync(normalizedEvents);

        stopwatch.Stop();
        var rejected = incoming.Count - normalizedEvents.Count;
        _metricsService?.RecordAnalyticsIngest(normalizedEvents.Count, Math.Max(0, rejected), stopwatch.Elapsed.TotalMilliseconds);

        return normalizedEvents.Count;
    }

    public async Task<UsageSummaryResponse> GetSummaryAsync(int days)
    {
        var safeDays = Math.Clamp(days, 1, 90);
        var to = DateTime.UtcNow;
        var from = to.AddDays(-safeDays);
        var events = (await _usageEventRepository.GetRangeAsync(from, to)).ToList();

        var total = events.Count;
        var uniqueUsers = events
            .Where(e => !string.Equals(e.Username, "anonymous", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Username)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .LongCount();

        var typeCounts = events
            .GroupBy(e => e.EventType)
            .Select(g => new EventTypeCountResponse(g.Key, g.LongCount()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var pathCounts = events
            .Where(e => !string.IsNullOrWhiteSpace(e.Path))
            .GroupBy(e => e.Path)
            .Select(g => new PathCountResponse(g.Key, g.LongCount()))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var dailySeries = BuildDailySeries(events, from, to);

        var deviceCounts = events
            .GroupBy(e => e.DeviceType)
            .Select(g => new DeviceTypeCountResponse(g.Key, g.LongCount()))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new UsageSummaryResponse(total, uniqueUsers, typeCounts, pathCounts, dailySeries, deviceCounts);
    }

    public async Task<IEnumerable<UsageEvent>> GetRawEventsAsync(int days)
    {
        var safeDays = Math.Clamp(days, 1, 90);
        var to = DateTime.UtcNow;
        var from = to.AddDays(-safeDays);
        return await _usageEventRepository.GetRangeAsync(from, to);
    }

    private static List<DailyEventCountResponse> BuildDailySeries(List<UsageEvent> events, DateTime from, DateTime to)
    {
        var eventsByDay = events
            .GroupBy(e => e.Timestamp.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Include every calendar day in the range so the sparkline renders a continuous
        // series without gaps, even on days with zero activity.
        var series = new List<DailyEventCountResponse>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            if (!eventsByDay.TryGetValue(date, out var dayEvents))
            {
                series.Add(new DailyEventCountResponse(date.ToString("yyyy-MM-dd"), 0, 0, 0, 0));
                continue;
            }

            var pageViews = dayEvents.LongCount(e => string.Equals(e.EventType, "page_view", StringComparison.OrdinalIgnoreCase));
            var reportViews = dayEvents.LongCount(e => string.Equals(e.EventType, "report_view", StringComparison.OrdinalIgnoreCase));
            var dayUniqueUsers = dayEvents
                .Where(e => !string.Equals(e.Username, "anonymous", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Username)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .LongCount();

            series.Add(new DailyEventCountResponse(date.ToString("yyyy-MM-dd"), dayEvents.Count, pageViews, reportViews, dayUniqueUsers));
        }

        return series;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.Trim();
        return trimmed.Length > 512 ? trimmed[..512] : trimmed;
    }

    private static string NormalizeDeviceType(string? deviceType)
    {
        if (string.IsNullOrWhiteSpace(deviceType))
        {
            return "unknown";
        }

        var trimmed = deviceType.Trim().ToLowerInvariant();
        return trimmed is "mobile" or "tablet" or "desktop" ? trimmed : "unknown";
    }

    private static string SerializeMetadata(Dictionary<string, string>? metadata)
    {
        if (metadata == null || metadata.Count == 0)
        {
            return string.Empty;
        }

        var safe = metadata
            .Take(20)
            .ToDictionary(
                kvp => kvp.Key.Length > 64 ? kvp.Key[..64] : kvp.Key,
                kvp =>
                {
                    var value = kvp.Value ?? string.Empty;
                    return value.Length > 256 ? value[..256] : value;
                },
                StringComparer.OrdinalIgnoreCase
            );

        return JsonSerializer.Serialize(safe);
    }

    public async Task<string> ExportCsvAsync(int days)
    {
        var rawEvents = (await GetRawEventsAsync(days)).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Timestamp,EventType,Username,Path,DeviceType");
        foreach (var e in rawEvents)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(e.Timestamp.ToString("o")),
                CsvEscape(e.EventType),
                CsvEscape(e.Username),
                CsvEscape(e.Path),
                CsvEscape(e.DeviceType)));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}