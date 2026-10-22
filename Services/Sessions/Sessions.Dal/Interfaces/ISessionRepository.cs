using Sessions.Domain.Models;

namespace Sessions.Dal.Interfaces;

public interface ISessionRepository : IGenericRepository<Session>
{
    Task<Session?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    Task<bool> HasOverlapAsync(
        int swapId, DateTime scheduledAt, int durationMinutes, int? exceptSessionId, CancellationToken ct = default);

    void SetOriginalRowVersion(Session session, byte[] rowVersion);
}
