using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Matching.Application.Common;
using Matching.Application.Common.Exceptions;
using Matching.Application.Common.Validation;
using Matching.Domain.Enums;
using Matching.Domain.Exceptions;
using Matching.Domain.Interfaces;

namespace Matching.Application.Matches.Commands.UpdateMatchStatus;

/// <summary>Користувач переглянув або відхилив пару. Повертає нову версію документа.</summary>
public sealed record UpdateMatchStatusCommand(string Id, int ExpectedVersion, string Status) : ICommand<int>;

public sealed class UpdateMatchStatusCommandValidator : AbstractValidator<UpdateMatchStatusCommand>
{
    public UpdateMatchStatusCommandValidator()
    {
        RuleFor(x => x.Id).MustBeObjectId();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).MustBeEnumName<UpdateMatchStatusCommand, MatchStatus>()
            .Must(s => s != nameof(MatchStatus.Suggested)).WithMessage("A match cannot return to Suggested");
    }
}

public sealed class UpdateMatchStatusCommandHandler(IMatchRepository matches)
    : IRequestHandler<UpdateMatchStatusCommand, int>
{
    public async Task<int> Handle(UpdateMatchStatusCommand cmd, CancellationToken ct)
    {
        var match = await matches.GetByIdAsync(ObjectId.Parse(cmd.Id), ct)
                    ?? throw new NotFoundException("Match", cmd.Id);

        if (match.Version != cmd.ExpectedVersion)
            throw new ConflictException(
                $"Match {cmd.Id} was modified (expected version {cmd.ExpectedVersion}, actual {match.Version})");

        match.ChangeStatus(Enum.Parse<MatchStatus>(cmd.Status));
        await matches.UpdateAsync(match, ct);
        return match.Version;
    }
}
