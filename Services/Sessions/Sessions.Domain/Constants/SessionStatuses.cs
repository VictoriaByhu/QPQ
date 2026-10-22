namespace Sessions.Domain.Constants;

public static class SessionStatuses
{
    public const string Planned = "Planned";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All = [Planned, Completed, Cancelled];
}
