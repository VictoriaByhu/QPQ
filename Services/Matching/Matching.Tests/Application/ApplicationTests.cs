using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using Matching.Application.Common.Behaviors;
using Matching.Application.Common.Exceptions;
using Matching.Application.Common.Models;
using Matching.Application.IndexEntries.Commands.CreateIndexEntry;
using Matching.Application.IndexEntries.Commands.UpdateIndexEntry;
using Matching.Application.Matches.Commands.GenerateMatches;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Exceptions;
using Matching.Domain.Interfaces;
using static Matching.Tests.TestData;

namespace Matching.Tests.Application;

public class ApplicationTests
{
    private static CreateIndexEntryCommand ValidCreate(int rating = 4, int userSkillId = 1) => new(
        userSkillId, U1, 1, "Програмування C#", "Offer", rating, "Active", 5,
        [new LanguageItem("uk", 5)], [new AvailabilityItem(1, "18:00", "20:00")]);

    private static CreateIndexEntryCommandValidator Validator(bool exists = false)
    {
        var repo = Substitute.For<IUserSkillIndexRepository>();
        repo.ExistsByUserSkillIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(exists);
        return new CreateIndexEntryCommandValidator(repo);
    }

    [Fact]
    public async Task Validation_behavior_rejects_invalid_command_without_calling_the_handler()
    {
        var behavior = new ValidationBehavior<CreateIndexEntryCommand, string>([Validator()]);
        var handlerCalled = false;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(
            ValidCreate(rating: 10), _ => { handlerCalled = true; return Task.FromResult("id"); }, default));

        Assert.False(handlerCalled);                                  // обробник не виконувався
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateIndexEntryCommand.Level));
    }

    [Fact]
    public async Task Validation_behavior_collects_all_errors_together()
    {
        var behavior = new ValidationBehavior<CreateIndexEntryCommand, string>([Validator()]);
        var bad = ValidCreate() with { Level = 0, UserId = "x", SkillName = "", Type = "Nope" };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(bad, _ => Task.FromResult("id"), default));

        Assert.True(ex.Errors.Count() >= 4);
    }

    [Fact]
    public async Task Valid_command_passes_through_to_the_handler()
    {
        var behavior = new ValidationBehavior<CreateIndexEntryCommand, string>([Validator()]);
        var result = await behavior.Handle(ValidCreate(), _ => Task.FromResult("done"), default);
        Assert.Equal("done", result);
    }

    [Fact]
    public async Task Async_validator_rejects_duplicate_user_skill_id()
    {
        var result = await Validator(exists: true).ValidateAsync(ValidCreate());
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task Want_with_experience_years_is_invalid()
    {
        var result = await Validator().ValidateAsync(ValidCreate() with { Type = "Want" });
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("only for Offer"));
    }

    [Fact]
    public async Task Availability_with_reversed_time_is_invalid()
    {
        var cmd = ValidCreate() with { Availability = [new AvailabilityItem(1, "20:00", "18:00")] };
        Assert.False((await Validator().ValidateAsync(cmd)).IsValid);
    }

    [Fact]
    public async Task Logging_behavior_logs_and_passes_the_response()
    {
        var behavior = new LoggingBehavior<CreateIndexEntryCommand, string>(NullLogger<LoggingBehavior<CreateIndexEntryCommand, string>>.Instance);
        Assert.Equal("ok", await behavior.Handle(ValidCreate(), _ => Task.FromResult("ok"), default));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(ValidCreate(), _ => throw new InvalidOperationException(), default));
    }

    [Fact]
    public async Task Update_with_stale_version_throws_ConflictException()
    {
        var entry = Entry();
        var repo = Substitute.For<IUserSkillIndexRepository>();
        repo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        var handler = new UpdateIndexEntryCommandHandler(repo);

        var cmd = new UpdateIndexEntryCommand(entry.Id.ToString(), ExpectedVersion: 5, "C#", 3, "Active", 1, [], []);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(cmd, default));
        await repo.DidNotReceive().UpdateAsync(Arg.Any<UserSkillIndexEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_of_missing_document_throws_NotFound()
    {
        var repo = Substitute.For<IUserSkillIndexRepository>();
        var handler = new UpdateIndexEntryCommandHandler(repo);
        var cmd = new UpdateIndexEntryCommand(ObjectId.GenerateNewId().ToString(), 0, "C#", 3, "Active", 1, [], []);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(cmd, default));
    }

    [Fact]
    public async Task GenerateMatches_finds_a_mutual_pair_and_stores_one_document()
    {
        var myWant = Entry(1, U2, 1, UserSkillType.Want, level: 2);                   // U2 хоче навичку 1
        var myOffer = Entry(2, U2, 2, UserSkillType.Offer, level: 5);                 // U2 пропонує навичку 2
        var theirOffer = Entry(3, U1, 1, UserSkillType.Offer, level: 4);              // U1 пропонує навичку 1
        var theirWant = Entry(4, U1, 2, UserSkillType.Want, level: 2);                // U1 хоче навичку 2

        var index = Substitute.For<IUserSkillIndexRepository>();
        index.FindActiveAsync(U2, null, UserSkillType.Want, null, Arg.Any<CancellationToken>()).Returns([myWant]);
        index.FindActiveAsync(U2, null, UserSkillType.Offer, null, Arg.Any<CancellationToken>()).Returns([myOffer]);
        index.FindActiveAsync(null, 1, UserSkillType.Offer, U2, Arg.Any<CancellationToken>()).Returns([theirOffer]);
        index.FindActiveAsync(U1, null, UserSkillType.Want, null, Arg.Any<CancellationToken>()).Returns([theirWant]);

        var matches = Substitute.For<IMatchRepository>();
        Match? stored = null;
        await matches.AddAsync(Arg.Do<Match>(m => stored = m), Arg.Any<CancellationToken>());

        var handler = new GenerateMatchesCommandHandler(index, matches, NullLogger<GenerateMatchesCommandHandler>.Instance);
        var result = await handler.Handle(new GenerateMatchesCommand(U2), default);

        Assert.Equal(new GenerateMatchesResult(1, 0, 0), result);
        Assert.NotNull(stored);
        Assert.Equal(MatchKind.Mutual, stored!.Kind);
        Assert.Equal(U1, stored.Offer.FromUserId);
        Assert.Equal(U2, stored.CounterOffer!.FromUserId);
    }

    [Fact]
    public async Task GenerateMatches_skips_an_existing_pair()
    {
        var want = Entry(1, U2, 1, UserSkillType.Want, level: 2);
        var offer = Entry(3, U1, 1, UserSkillType.Offer, level: 4);
        var index = Substitute.For<IUserSkillIndexRepository>();
        index.FindActiveAsync(U2, null, UserSkillType.Want, null, Arg.Any<CancellationToken>()).Returns([want]);
        index.FindActiveAsync(U2, null, UserSkillType.Offer, null, Arg.Any<CancellationToken>()).Returns([]);
        index.FindActiveAsync(null, 1, UserSkillType.Offer, U2, Arg.Any<CancellationToken>()).Returns([offer]);
        index.FindActiveAsync(U1, null, UserSkillType.Want, null, Arg.Any<CancellationToken>()).Returns([]);

        var matches = Substitute.For<IMatchRepository>();
        matches.GetByOfferKeyAsync(U1, U2, 1, Arg.Any<CancellationToken>())
            .Returns(Match.OneWay(Offer(U1, U2), 60, ["uk"]));

        var handler = new GenerateMatchesCommandHandler(index, matches, NullLogger<GenerateMatchesCommandHandler>.Instance);
        var result = await handler.Handle(new GenerateMatchesCommand(U2), default);

        Assert.Equal(new GenerateMatchesResult(0, 0, 1), result);
        await matches.DidNotReceive().AddAsync(Arg.Any<Match>(), Arg.Any<CancellationToken>());
    }
}
