using FluentValidation;
using MediatR;
using Matching.Application.Common;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;
using Matching.Application.Common.Validation;
using Matching.Domain.Enums;

namespace Matching.Application.IndexEntries.Queries.GetIndexEntriesList;

public sealed record GetIndexEntriesListQuery(
    string? UserId, int? SkillId, string? Type, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<IndexEntryDto>>;

public sealed class GetIndexEntriesListQueryValidator : AbstractValidator<GetIndexEntriesListQuery>
{
    public GetIndexEntriesListQueryValidator()
    {
        RuleFor(x => x.UserId!).MustBeUserId().When(x => x.UserId is not null);
        RuleFor(x => x.SkillId).GreaterThan(0).When(x => x.SkillId is not null);
        RuleFor(x => x.Type!).MustBeEnumName<GetIndexEntriesListQuery, UserSkillType>().When(x => x.Type is not null);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).MustBePageSize();
    }
}

public sealed class GetIndexEntriesListQueryHandler(IUserSkillIndexQueries queries)
    : IRequestHandler<GetIndexEntriesListQuery, PagedResult<IndexEntryDto>>
{
    public Task<PagedResult<IndexEntryDto>> Handle(GetIndexEntriesListQuery q, CancellationToken ct)
        => queries.GetListAsync(q.UserId, q.SkillId, q.Type, q.Page, Math.Clamp(q.PageSize, 1, 100), ct);
}
