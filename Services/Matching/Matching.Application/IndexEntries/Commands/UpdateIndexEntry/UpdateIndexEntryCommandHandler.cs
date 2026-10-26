using MediatR;
using MongoDB.Bson;
using Matching.Application.Common.Exceptions;
using Matching.Application.Common.Mapping;
using Matching.Domain.Enums;
using Matching.Domain.Exceptions;
using Matching.Domain.Interfaces;

namespace Matching.Application.IndexEntries.Commands.UpdateIndexEntry;

public sealed class UpdateIndexEntryCommandHandler(IUserSkillIndexRepository index)
    : IRequestHandler<UpdateIndexEntryCommand, int>
{
    public async Task<int> Handle(UpdateIndexEntryCommand cmd, CancellationToken ct)
    {
        var entry = await index.GetByIdAsync(ObjectId.Parse(cmd.Id), ct)
                    ?? throw new NotFoundException("Index entry", cmd.Id);

        if (entry.Version != cmd.ExpectedVersion)
            throw new ConflictException(
                $"Index entry {cmd.Id} was modified (expected version {cmd.ExpectedVersion}, actual {entry.Version})");

        entry.Update(cmd.SkillName, cmd.Level, Enum.Parse<UserSkillStatus>(cmd.Status),
            cmd.ExperienceYears, cmd.Languages.ToDomain(), cmd.Availability.ToDomain());

        await index.UpdateAsync(entry, ct);          // перевіряє версію ще раз на боці БД
        return entry.Version;
    }
}
