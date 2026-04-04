using System.Diagnostics.Metrics;

namespace ReportTree.Server.Services;

public sealed class MetricsService : IDisposable
{
    public const string MeterName = "ReportTree.BusinessMetrics";

    private readonly Meter _meter;
    private readonly Counter<long> _analyticsEventsAccepted;
    private readonly Counter<long> _analyticsEventsRejected;
    private readonly Histogram<double> _analyticsIngestDurationMs;
    private readonly Counter<long> _embedTokensGenerated;
    private readonly Histogram<double> _embedTokenGenerationDurationMs;

    public MetricsService()
    {
        _meter = new Meter(MeterName, "1.0.0");
        _analyticsEventsAccepted = _meter.CreateCounter<long>("analytics.events.accepted");
        _analyticsEventsRejected = _meter.CreateCounter<long>("analytics.events.rejected");
        _analyticsIngestDurationMs = _meter.CreateHistogram<double>("analytics.ingest.duration.ms", unit: "ms");
        _embedTokensGenerated = _meter.CreateCounter<long>("powerbi.embed_tokens.generated");
        _embedTokenGenerationDurationMs = _meter.CreateHistogram<double>("powerbi.embed_tokens.generation.duration.ms", unit: "ms");
    }

    public void RecordAnalyticsIngest(int accepted, int rejected, double durationMs)
    {
        if (accepted > 0)
        {
            _analyticsEventsAccepted.Add(accepted);
        }

        if (rejected > 0)
        {
            _analyticsEventsRejected.Add(rejected);
        }

        _analyticsIngestDurationMs.Record(Math.Max(0, durationMs));
    }

    public void RecordEmbedTokenGenerated(string embedType, double durationMs)
    {
        var tag = KeyValuePair.Create<string, object?>("embed_type", embedType);
        _embedTokensGenerated.Add(1, tag);
        _embedTokenGenerationDurationMs.Record(Math.Max(0, durationMs), tag);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
