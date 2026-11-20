using Sessions.Domain.Constants;
using Sessions.Domain.Exceptions;

namespace Sessions.Domain.Models;

public class AcceptedSwap
{
    public const int StatusMaxLength = 20;

    private readonly List<Session> _sessions = [];

    public int SwapId { get; private set; }
    public Guid InitiatorId { get; private set; }
    public Guid PartnerId { get; private set; }
    public string Status { get; private set; } = AcceptedSwapStatuses.Accepted;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>When the swap reached its current status in Swaps; null for rows seeded before replication.</summary>
    public DateTime? StatusChangedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<Session> Sessions => _sessions;

    private AcceptedSwap() { }

    public AcceptedSwap(int swapId, Guid initiatorId, Guid partnerId, string status = AcceptedSwapStatuses.Accepted)
    {
        if (swapId <= 0)
            throw new DomainException("SwapId must be positive.");
        if (initiatorId == Guid.Empty || partnerId == Guid.Empty)
            throw new DomainException("Swap participants must be specified.");
        if (initiatorId == partnerId)
            throw new DomainException("Initiator and partner must be different users.");

        SwapId = swapId;
        InitiatorId = initiatorId;
        PartnerId = partnerId;
        SetStatus(status);
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsParticipant(Guid userId) => InitiatorId == userId || PartnerId == userId;

    public void EnsureCanPlanSessions()
    {
        if (Status != AcceptedSwapStatuses.Accepted)
            throw new DomainException($"Swap {SwapId} is {Status}; sessions can be planned only for accepted swaps.");
    }

    public void ChangeStatus(string status)
    {
        SetStatus(status);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Applies a status replicated from Swaps unless a newer change is already applied,
    /// so a late or redelivered event never rolls the status back. Returns whether anything changed.
    /// </summary>
    public bool ApplyReplicatedStatus(string status, DateTime changedAt)
    {
        if (StatusChangedAt is { } applied && applied >= changedAt)
            return false;

        SetStatus(status);
        StatusChangedAt = changedAt;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private void SetStatus(string status)
    {
        if (!AcceptedSwapStatuses.All.Contains(status))
            throw new DomainException($"Swap status must be one of: {string.Join(", ", AcceptedSwapStatuses.All)}.");

        Status = status;
    }
}
