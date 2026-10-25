using Messages.Domain.Common;
using Messages.Domain.Exceptions;

namespace Messages.Domain.ValueObjects;

public sealed class SystemEventDetails : ValueObject
{
    public int SwapId { get; }

    public SystemEventDetails(int swapId)
    {
        if (swapId <= 0)
            throw new DomainException("SwapId must be positive.");
        SwapId = swapId;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SwapId;
    }
}
