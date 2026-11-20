using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Platform.ServiceDefaults.Metrics;

namespace Swaps.Api.Metrics;

/// <summary>Business metrics of the Swaps service. Tags are low-cardinality only: no user, swap or correlation ids.</summary>
public sealed class SwapMetrics
{
    public const string MeterName = "Swaps.Api";

    private const string StatusTag = "swap.status";

    private readonly Counter<long> _created;
    private readonly Counter<long> _statusChanged;

    public SwapMetrics(IMeterFactory factory, IMemoryCache memoryCache)
    {
        var meter = factory.Create(MeterName);

        _created = meter.CreateCounter<long>("swaps.created", unit: "{swap}",
            description: "Swaps created, by initial status.");
        _statusChanged = meter.CreateCounter<long>("swaps.status_changed", unit: "{swap}",
            description: "Swap status transitions, by new status.");
        Duration = meter.CreateDurationHistogram("swaps.operation.duration",
            "Duration of swap operations, by operation and outcome.");

        // Reads in-process statistics only, so a collection never touches the database.
        meter.CreateObservableGauge("swaps.cache.entries",
            () => memoryCache.GetCurrentStatistics()?.CurrentEntryCount ?? 0,
            unit: "{entry}",
            description: "Entries currently held in the in-memory (L1) cache.");
    }

    public Histogram<double> Duration { get; }

    public void Created(string status) => _created.Add(1, new KeyValuePair<string, object?>(StatusTag, status));

    public void StatusChanged(string status) => _statusChanged.Add(1, new KeyValuePair<string, object?>(StatusTag, status));
}
