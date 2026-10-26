using MediatR;
using Matching.Application.Common.Mapping;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Interfaces;

namespace Matching.Application.IndexEntries.Commands.CreateIndexEntry;

public sealed class CreateIndexEntryCommandHandler(IUserSkillIndexRepository index)
    : IRequestHandler<CreateIndexEntryCommand, string>
{
    public async Task<string> Handle(CreateIndexEntryCommand cmd, CancellationToken ct)
    {
        var entry = new UserSkillIndexEntry(
            cmd.UserSkillId, cmd.UserId, cmd.SkillId, cmd.SkillName,
            Enum.Parse<UserSkillType>(cmd.Type), cmd.Level, Enum.Parse<UserSkillStatus>(cmd.Status),
            cmd.ExperienceYears, cmd.Languages.ToDomain(), cmd.Availability.ToDomain());

        await index.AddAsync(entry, ct);
        return entry.Id.ToString();
    }
}
