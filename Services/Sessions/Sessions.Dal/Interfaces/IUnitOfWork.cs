using Sessions.Domain.Models;

namespace Sessions.Dal.Interfaces;

public interface IUnitOfWork
{
    ISessionRepository Sessions { get; }
    IAcceptedSwapRepository AcceptedSwaps { get; }
    ISessionSkillRepository SessionSkills { get; }
    IGenericRepository<Skill> Skills { get; }
    IGenericRepository<SessionFormat> Formats { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
