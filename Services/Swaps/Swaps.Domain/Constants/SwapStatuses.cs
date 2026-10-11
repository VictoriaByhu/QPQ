namespace Swaps.Domain.Constants;

public static class SwapStatuses
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending, Accepted, Completed, Rejected, Cancelled
    };
}
