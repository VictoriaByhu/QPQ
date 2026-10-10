namespace Swaps.Domain.Models;

public class SwapDetails
{
    public int SwapId { get; set; }
    public string? Message { get; set; }
    public string? Location { get; set; }
    public DateTime? ProposedDate { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
