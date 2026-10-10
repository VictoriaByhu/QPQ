namespace Swaps.Domain.Models;

public class Swap
{
    public int Id { get; set; }
    public Guid InitiatorId { get; set; }
    public Guid PartnerId { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public SwapDetails? Details { get; set; }
    public List<SwapSkill> Skills { get; set; } = [];
}
