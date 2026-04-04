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
        "widget_interaction"
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

        return new UsageSummaryResponse(total, uniqueUsers, typeCounts, pathCounts);
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
}