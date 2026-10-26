using FluentValidation;
using MediatR;
using Matching.Application.Common;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;
using Matching.Application.Common.Validation;

namespace Matching.Application.Matches.Queries.GetMatchesList;

/// <summary>Пари користувача за спаданням оцінки; відхилені приховані, якщо не запрошено інше.</summary>
public sealed record GetMatchesListQuery(string UserId, bool IncludeDismissed = false, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<MatchDto>>;

public sealed class GetMatchesListQueryValidator : AbstractValidator<GetMatchesListQuery>
{
    public GetMatchesListQueryValidator()
    {
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).MustBePageSize();
    }
}

public sealed class GetMatchesListQueryHandler(IMatchQueries queries)
    : IRequestHandler<GetMatchesListQuery, PagedResult<MatchDto>>
{
    public Task<PagedResult<MatchDto>> Handle(GetMatchesListQuery q, CancellationToken ct)
        => queries.GetForUserAsync(q.UserId, q.IncludeDismissed, q.Page, Math.Clamp(q.PageSize, 1, 100), ct);
}
