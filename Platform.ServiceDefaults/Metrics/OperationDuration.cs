using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.ServiceDefaults.Metrics;

/// <summary>Records how long a business operation took, for failed operations as well.</summary>
public static class OperationDuration
{
    public const string OperationTag = "operation";
    public const string StatusTag = "status";
    public const string Success = "success";
    public const string Failure = "failure";

    /// <summary>Bucket boundaries in milliseconds, from a cache hit to a slow database call.</summary>
    public static readonly InstrumentAdvice<double> Buckets = new()
    {
        HistogramBucketBoundaries = [1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000]
    };

    public static Histogram<double> CreateDurationHistogram(this Meter meter, string name, string description)
        => meter.CreateHistogram(name, unit: "ms", description: description, tags: null, advice: Buckets);

    public static async Task<T> MeasureAsync<T>(this Histogram<double> histogram, string operation, Func<Task<T>> action)
    {
        var started = Stopwatch.GetTimestamp();
        var status = Failure;
        try
        {
            var result = await action();
            status = Success;
            return result;
        }
        finally
        {
            histogram.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>(OperationTag, operation),
                new KeyValuePair<string, object?>(StatusTag, status));
        }
    }

    public static Task MeasureAsync(this Histogram<double> histogram, string operation, Func<Task> action)
        => histogram.MeasureAsync(operation, async () =>
        {
            await action();
            return true;
        });
}
