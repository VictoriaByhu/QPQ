using FluentValidation;
using MediatR;
using Matching.Application.Common;
using Matching.Application.Common.Exceptions;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;
using Matching.Application.Common.Validation;

namespace Matching.Application.IndexEntries.Queries.GetIndexEntryById;

public sealed record GetIndexEntryByIdQuery(string Id) : IQuery<IndexEntryDto>;

public sealed class GetIndexEntryByIdQueryValidator : AbstractValidator<GetIndexEntryByIdQuery>
{
    public GetIndexEntryByIdQueryValidator() => RuleFor(x => x.Id).MustBeObjectId();
}

public sealed class GetIndexEntryByIdQueryHandler(IUserSkillIndexQueries queries)
    : IRequestHandler<GetIndexEntryByIdQuery, IndexEntryDto>
{
    public async Task<IndexEntryDto> Handle(GetIndexEntryByIdQuery q, CancellationToken ct)
        => await queries.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException("Index entry", q.Id);
}
