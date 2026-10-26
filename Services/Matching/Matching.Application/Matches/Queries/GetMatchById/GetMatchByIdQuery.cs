using FluentValidation;
using MediatR;
using Matching.Application.Common;
using Matching.Application.Common.Exceptions;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;
using Matching.Application.Common.Validation;

namespace Matching.Application.Matches.Queries.GetMatchById;

public sealed record GetMatchByIdQuery(string Id) : IQuery<MatchDto>;

public sealed class GetMatchByIdQueryValidator : AbstractValidator<GetMatchByIdQuery>
{
    public GetMatchByIdQueryValidator() => RuleFor(x => x.Id).MustBeObjectId();
}

public sealed class GetMatchByIdQueryHandler(IMatchQueries queries) : IRequestHandler<GetMatchByIdQuery, MatchDto>
{
    public async Task<MatchDto> Handle(GetMatchByIdQuery q, CancellationToken ct)
        => await queries.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException("Match", q.Id);
}
