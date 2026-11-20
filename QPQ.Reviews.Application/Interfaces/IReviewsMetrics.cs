namespace QPQ.Reviews.Application.Interfaces;

public interface IReviewsMetrics
{
    void ReviewCreated(int rating);
    void RecordDuration(string operation, bool success, double durationMs);
    void IncrementActiveOperations();
    void DecrementActiveOperations();
}