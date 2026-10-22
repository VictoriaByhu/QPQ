using Ardalis.Specification;
using Sessions.Domain.Models;

namespace Sessions.Bll.Specifications;

public sealed class AcceptedSwapsOfUserSpec : Specification<AcceptedSwap>
{
    public AcceptedSwapsOfUserSpec(Guid userId, string? status)
    {
        Query.Where(a => a.InitiatorId == userId || a.PartnerId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            Query.Where(a => a.Status == status);

        Query.OrderBy(a => a.SwapId).AsNoTracking();
    }
}
