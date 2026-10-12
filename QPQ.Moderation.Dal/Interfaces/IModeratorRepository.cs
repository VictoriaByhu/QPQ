using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Dal.Interfaces;

public interface IModeratorRepository
{
    Task<Moderator?> GetByIdAsync(long id, CancellationToken ct = default);
}