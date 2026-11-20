using Platform.ServiceDefaults.Metrics;
using Sessions.Bll.Dtos;
using Sessions.Bll.Services;

namespace Sessions.Api.Metrics;

/// <summary>Decorator that measures session operations and counts successful ones; the logic stays in the BLL.</summary>
public sealed class MeteredSessionService(SessionService inner, SessionMetrics metrics) : ISessionService
{
    public async Task<SessionDto> CreateAsync(Guid userId, CreateSessionDto input, CancellationToken ct = default)
    {
        var created = await metrics.Duration.MeasureAsync("create", () => inner.CreateAsync(userId, input, ct));
        metrics.Created(created.FormatCode);
        return created;
    }

    public async Task<SessionDto> UpdateAsync(Guid userId, int sessionId, UpdateSessionDto input, CancellationToken ct = default)
    {
        var updated = await metrics.Duration.MeasureAsync("reschedule", () => inner.UpdateAsync(userId, sessionId, input, ct));
        metrics.Rescheduled(updated.FormatCode);
        return updated;
    }

    public Task<SessionDto> CompleteAsync(Guid userId, int sessionId, CompleteSessionDto input, CancellationToken ct = default)
        => metrics.Duration.MeasureAsync("complete", () => inner.CompleteAsync(userId, sessionId, input, ct));

    public Task CancelAsync(Guid userId, int sessionId, ChangeSessionStatusDto? input, CancellationToken ct = default)
        => metrics.Duration.MeasureAsync("cancel", () => inner.CancelAsync(userId, sessionId, input, ct));

    public Task<SessionDto> GetByIdAsync(Guid userId, int sessionId, CancellationToken ct = default)
        => metrics.Duration.MeasureAsync("get", () => inner.GetByIdAsync(userId, sessionId, ct));

    public Task<PagedResult<SessionListItemDto>> GetListAsync(Guid userId, SessionQueryDto query, CancellationToken ct = default)
        => inner.GetListAsync(userId, query, ct);

    public Task<IReadOnlyList<SessionListItemDto>> GetUpcomingAsync(Guid userId, int take, CancellationToken ct = default)
        => inner.GetUpcomingAsync(userId, take, ct);

    public Task DeleteAsync(Guid userId, int sessionId, CancellationToken ct = default)
        => inner.DeleteAsync(userId, sessionId, ct);

    public Task<SessionDto> AddSkillAsync(Guid userId, int sessionId, AddSessionSkillDto input, CancellationToken ct = default)
        => inner.AddSkillAsync(userId, sessionId, input, ct);

    public Task RemoveSkillAsync(Guid userId, int sessionId, int skillId, CancellationToken ct = default)
        => inner.RemoveSkillAsync(userId, sessionId, skillId, ct);
}
