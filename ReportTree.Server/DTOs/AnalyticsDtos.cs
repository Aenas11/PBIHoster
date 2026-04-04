using System.ComponentModel.DataAnnotations;

namespace ReportTree.Server.DTOs;

public record UsageEventRequest(
    [Required] string EventType,
    string? Path,
    string? DeviceType,
    Dictionary<string, string>? Metadata
);

public record UsageEventIngestRequest(
    [Required] List<UsageEventRequest> Events
);

public record UsageSummaryResponse(
    long TotalEvents,
    long UniqueUsers,
    List<EventTypeCountResponse> EventTypes,
    List<PathCountResponse> TopPaths
);

public record EventTypeCountResponse(string EventType, long Count);

public record PathCountResponse(string Path, long Count);