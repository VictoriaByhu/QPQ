using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Matching.Application.Common;
using Matching.Application.Common.Exceptions;
using Matching.Application.Common.Validation;
using Matching.Domain.Interfaces;

namespace Matching.Application.IndexEntries.Commands.DeleteIndexEntry;

/// <summary>Видаляє запис індексу (подія UserSkillsChanged або UserDeleted).</summary>
public sealed record DeleteIndexEntryCommand(string Id) : ICommand;

public sealed class DeleteIndexEntryCommandValidator : AbstractValidator<DeleteIndexEntryCommand>
{
    public DeleteIndexEntryCommandValidator() => RuleFor(x => x.Id).MustBeObjectId();
}

public sealed class DeleteIndexEntryCommandHandler(IUserSkillIndexRepository index)
    : IRequestHandler<DeleteIndexEntryCommand>
{
    public async Task Handle(DeleteIndexEntryCommand cmd, CancellationToken ct)
    {
        if (!await index.DeleteAsync(ObjectId.Parse(cmd.Id), ct))
            throw new NotFoundException("Index entry", cmd.Id);
    }
}
