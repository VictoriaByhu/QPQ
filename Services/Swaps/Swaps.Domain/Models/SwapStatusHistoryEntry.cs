namespace Swaps.Domain.Models;

public class SwapStatusHistoryEntry
{
    public long Id { get; set; }
    public int SwapId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = "";
    public Guid ChangedByUserId { get; set; }
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
}
