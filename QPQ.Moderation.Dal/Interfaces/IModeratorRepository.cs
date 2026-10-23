using QPQ.Moderation.Domain.Entities;
using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Dal.Interfaces;

public interface IModeratorRepository : IRepository<Moderator>
{
    // Explicit Loading: завантажуЇмо модератора, а його скарги Ч окремим запитом за потреби
    Task<Moderator?> GetModeratorWithAssignmentsAsync(int id, CancellationToken ct = default);
}