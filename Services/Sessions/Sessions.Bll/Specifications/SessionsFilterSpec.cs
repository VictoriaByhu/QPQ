using System.Linq.Expressions;
using Ardalis.Specification;
using Sessions.Bll.Dtos;
using Sessions.Domain.Models;

namespace Sessions.Bll.Specifications;

public sealed class SessionsFilterSpec : Specification<Session>
{
    public static readonly IReadOnlyList<string> SortFields = ["scheduledAt", "duration", "status", "createdAt"];

    public SessionsFilterSpec(Guid userId, SessionQueryDto q)
    {
        Query.Include(s => s.Format).AsNoTracking();

        Query.Where(s => s.Swap.InitiatorId == userId || s.Swap.PartnerId == userId);

        if (q.SwapId is { } swapId)
            Query.Where(s => s.SwapId == swapId);

        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            var status = q.Status;
            Query.Where(s => s.Status == status);
        }

        if (q.FormatId is { } formatId)
            Query.Where(s => s.FormatId == formatId);

        if (q.SkillId is { } skillId)
            Query.Where(s => s.Skills.Any(ss => ss.SkillId == skillId));

        if (q.From is { } from)
            Query.Where(s => s.ScheduledAt >= from);

        if (q.To is { } to)
            Query.Where(s => s.ScheduledAt <= to);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim();
            Query.Where(s => (s.Location != null && s.Location.Contains(search))
                             || (s.Link != null && s.Link.Contains(search)));
        }

        var key = SortKey(q.SortBy);
        if (string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase))
            Query.OrderByDescending(key).ThenByDescending(s => s.Id);
        else
            Query.OrderBy(key).ThenBy(s => s.Id);

        Query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize);
    }

    private static Expression<Func<Session, object?>> SortKey(string? sortBy) => sortBy?.ToLowerInvariant() switch
    {
        "duration" => s => s.DurationMinutes,
        "status" => s => s.Status,
        "createdat" => s => s.CreatedAt,
        _ => s => s.ScheduledAt
    };
}
