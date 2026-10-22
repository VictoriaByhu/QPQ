namespace Sessions.Domain.Constants;

public static class AcceptedSwapStatuses
{
    public const string Accepted = "Accepted";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All = [Accepted, Completed, Cancelled];
}
