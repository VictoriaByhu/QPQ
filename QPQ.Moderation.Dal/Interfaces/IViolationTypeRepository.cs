using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Dal.Interfaces;

public interface IViolationTypeRepository
{
    Task<ViolationType?> GetByIdAsync(long id, CancellationToken ct = default);
}