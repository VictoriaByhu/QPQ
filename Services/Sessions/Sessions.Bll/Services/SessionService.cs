using AutoMapper;
using Sessions.Bll.Dtos;
using Sessions.Bll.Specifications;
using Sessions.Dal.Interfaces;
using Sessions.Domain.Exceptions;
using Sessions.Domain.Models;

namespace Sessions.Bll.Services;

public sealed class SessionService(IUnitOfWork uow, IMapper mapper) : ISessionService
{
    public async Task<PagedResult<SessionListItemDto>> GetListAsync(
        Guid userId, SessionQueryDto query, CancellationToken ct = default)
    {
        var spec = new SessionsFilterSpec(userId, query);
        var total = await uow.Sessions.CountAsync(spec, ct);
        var items = await uow.Sessions.ListAsync(spec, ct);

        return new PagedResult<SessionListItemDto>(
            mapper.Map<List<SessionListItemDto>>(items), total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<SessionListItemDto>> GetUpcomingAsync(
        Guid userId, int take, CancellationToken ct = default)
    {
        if (take is < 1 or > 50)
            throw new ValidationException("Take must be between 1 and 50.");

        var sessions = await uow.Sessions.ListAsync(new UpcomingSessionsSpec(userId, DateTime.UtcNow, take), ct);
        return mapper.Map<List<SessionListItemDto>>(sessions);
    }

    public async Task<SessionDto> GetByIdAsync(Guid userId, int sessionId, CancellationToken ct = default)
        => mapper.Map<SessionDto>(await LoadParticipantSessionAsync(userId, sessionId, ct));

    public async Task<SessionDto> CreateAsync(Guid userId, CreateSessionDto input, CancellationToken ct = default)
    {
        var swap = await uow.AcceptedSwaps.GetByIdAsync(input.SwapId, ct);
        if (swap is null || !swap.IsParticipant(userId))
            throw new NotFoundException($"Swap {input.SwapId} not found");
        swap.EnsureCanPlanSessions();

        await EnsureFormatExistsAsync(input.FormatId, ct);
        await EnsureSkillsExistAsync(input.Skills.Select(s => s.SkillId).Distinct().ToList(), ct);

        var scheduledAt = input.ScheduledAt!.Value;
        if (await uow.Sessions.HasOverlapAsync(swap.SwapId, scheduledAt, input.DurationMinutes, null, ct))
            throw new BusinessConflictException("The swap already has a planned session at this time.");

        var session = new Session(swap.SwapId, input.FormatId, scheduledAt, input.DurationMinutes,
            input.Link, input.Location, userId);
        foreach (var skill in input.Skills)
            session.AddSkill(skill.SkillId, skill.PlannedMinutes);

        await uow.Sessions.AddAsync(session, ct);
        await uow.SaveChangesAsync(ct);

        return await GetByIdAsync(userId, session.Id, ct);
    }

    public async Task<SessionDto> UpdateAsync(
        Guid userId, int sessionId, UpdateSessionDto input, CancellationToken ct = default)
    {
        var session = await LoadParticipantSessionAsync(userId, sessionId, ct);
        ApplyRowVersion(session, input.RowVersion);
        session.Swap.EnsureCanPlanSessions();

        await EnsureFormatExistsAsync(input.FormatId, ct);

        var scheduledAt = input.ScheduledAt!.Value;
        if (await uow.Sessions.HasOverlapAsync(session.SwapId, scheduledAt, input.DurationMinutes, session.Id, ct))
            throw new BusinessConflictException("The swap already has a planned session at this time.");

        session.Reschedule(scheduledAt, input.DurationMinutes);
        session.ChangePlace(input.FormatId, input.Link, input.Location);
        await uow.SaveChangesAsync(ct);

        return mapper.Map<SessionDto>(session);
    }

    public async Task<SessionDto> CompleteAsync(
        Guid userId, int sessionId, CompleteSessionDto input, CancellationToken ct = default)
    {
        var session = await LoadParticipantSessionAsync(userId, sessionId, ct);
        ApplyRowVersion(session, input.RowVersion);

        session.Complete(input.Summary, input.ActualDurationMinutes, DateTime.UtcNow);
        await uow.SaveChangesAsync(ct);

        return mapper.Map<SessionDto>(session);
    }

    public async Task CancelAsync(
        Guid userId, int sessionId, ChangeSessionStatusDto? input, CancellationToken ct = default)
    {
        var session = await LoadParticipantSessionAsync(userId, sessionId, ct);
        ApplyRowVersion(session, input?.RowVersion);

        session.Cancel();
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, int sessionId, CancellationToken ct = default)
    {
        var session = await LoadParticipantSessionAsync(userId, sessionId, ct);

        session.Delete();
        await uow.SaveChangesAsync(ct);
    }

    public async Task<SessionDto> AddSkillAsync(
        Guid userId, int sessionId, AddSessionSkillDto input, CancellationToken ct = default)
    {
        var session = await LoadParticipantSessionAsync(userId, sessionId, ct);
        ApplyRowVersion(session, input.RowVersion);

        var skill = await uow.Skills.GetByIdAsync(input.SkillId, ct);
        if (skill is null || skill.IsDeleted)
            throw new NotFoundException($"Skill {input.SkillId} not found");

        session.AddSkill(skill.Id, input.PlannedMinutes);
        await uow.SaveChangesAsync(ct);

        return mapper.Map<SessionDto>(session);
    }

    public async Task RemoveSkillAsync(Guid userId, int sessionId, int skillId, CancellationToken ct = default)
    {
        var session = await LoadParticipantSessionAsync(userId, sessionId, ct);

        session.RemoveSkill(skillId);
        await uow.SaveChangesAsync(ct);
    }

    private async Task<Session> LoadParticipantSessionAsync(Guid userId, int sessionId, CancellationToken ct)
    {
        var session = await uow.Sessions.GetWithDetailsAsync(sessionId, ct);
        if (session is null || !session.Swap.IsParticipant(userId))
            throw new NotFoundException($"Session {sessionId} not found");

        return session;
    }

    private async Task EnsureFormatExistsAsync(byte formatId, CancellationToken ct)
    {
        if (await uow.Formats.GetByIdAsync(formatId, ct) is null)
            throw new NotFoundException($"Session format {formatId} not found");
    }

    private async Task EnsureSkillsExistAsync(IReadOnlyCollection<int> skillIds, CancellationToken ct)
    {
        if (skillIds.Count == 0)
            return;

        var found = await uow.Skills.ListAsync(new SkillsByIdsSpec(skillIds), ct);
        var missing = skillIds.Except(found.Select(s => s.Id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Skill(s) not found: {string.Join(", ", missing)}");
    }

    private void ApplyRowVersion(Session session, string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
            return;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new ValidationException("RowVersion has an invalid format.");
        }

        if (bytes.Length != 8)
            throw new ValidationException("RowVersion has an invalid format.");

        uow.Sessions.SetOriginalRowVersion(session, bytes);
    }
}
