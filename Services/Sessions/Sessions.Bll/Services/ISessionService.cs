using Sessions.Bll.Dtos;

namespace Sessions.Bll.Services;

public interface ISessionService
{
    Task<PagedResult<SessionListItemDto>> GetListAsync(Guid userId, SessionQueryDto query, CancellationToken ct = default);
    Task<IReadOnlyList<SessionListItemDto>> GetUpcomingAsync(Guid userId, int take, CancellationToken ct = default);
    Task<SessionDto> GetByIdAsync(Guid userId, int sessionId, CancellationToken ct = default);
    Task<SessionDto> CreateAsync(Guid userId, CreateSessionDto input, CancellationToken ct = default);
    Task<SessionDto> UpdateAsync(Guid userId, int sessionId, UpdateSessionDto input, CancellationToken ct = default);
    Task<SessionDto> CompleteAsync(Guid userId, int sessionId, CompleteSessionDto input, CancellationToken ct = default);
    Task CancelAsync(Guid userId, int sessionId, ChangeSessionStatusDto? input, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, int sessionId, CancellationToken ct = default);
    Task<SessionDto> AddSkillAsync(Guid userId, int sessionId, AddSessionSkillDto input, CancellationToken ct = default);
    Task RemoveSkillAsync(Guid userId, int sessionId, int skillId, CancellationToken ct = default);
}
