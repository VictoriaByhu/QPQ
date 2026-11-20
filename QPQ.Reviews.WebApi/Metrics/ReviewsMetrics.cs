using System.Diagnostics.Metrics;
using QPQ.Reviews.Application.Interfaces;

namespace QPQ.Reviews.WebApi.Metrics;

public class ReviewsMetrics : IReviewsMetrics
{
    private readonly Counter<long> _reviewsCreated;
    private readonly Histogram<double> _operationDuration;
    private int _activeOperations;

    public ReviewsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Reviews.Api");

        _reviewsCreated = meter.CreateCounter<long>(
            name: "reviews.created",
            unit: "{review}",
            description: "Number of created reviews");

        _operationDuration = meter.CreateHistogram<double>(
            name: "reviews.operation.duration",
            unit: "ms",
            description: "Duration of review operations");

        meter.CreateObservableGauge(
            name: "reviews.active_operations",
            observeValue: () => Volatile.Read(ref _activeOperations),
            unit: "{operation}",
            description: "Current number of in-flight review operations");
    }

    public void ReviewCreated(int rating) =>
        _reviewsCreated.Add(1, new KeyValuePair<string, object?>("review.rating", rating));

    public void RecordDuration(string operation, bool success, double durationMs) =>
        _operationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));

    public void IncrementActiveOperations() => Interlocked.Increment(ref _activeOperations);

    public void DecrementActiveOperations() => Interlocked.Decrement(ref _activeOperations);
}