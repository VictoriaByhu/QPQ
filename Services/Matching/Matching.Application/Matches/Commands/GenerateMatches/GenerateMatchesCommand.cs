using FluentValidation;
using MediatR;
using Matching.Application.Common;
using Matching.Application.Common.Models;
using Matching.Application.Common.Validation;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Interfaces;
using Matching.Domain.Services;
using Matching.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Matching.Application.Matches.Commands.GenerateMatches;

/// <summary>Підбирає пари для користувача: його бажання проти чужих пропозицій (і навпаки для взаємності).</summary>
public sealed record GenerateMatchesCommand(string UserId) : ICommand<GenerateMatchesResult>;

public sealed class GenerateMatchesCommandValidator : AbstractValidator<GenerateMatchesCommand>
{
    public GenerateMatchesCommandValidator() => RuleFor(x => x.UserId).MustBeUserId();
}

public sealed class GenerateMatchesCommandHandler(
    IUserSkillIndexRepository index,
    IMatchRepository matches,
    ILogger<GenerateMatchesCommandHandler> logger)
    : IRequestHandler<GenerateMatchesCommand, GenerateMatchesResult>
{
    public async Task<GenerateMatchesResult> Handle(GenerateMatchesCommand cmd, CancellationToken ct)
    {
        int created = 0, upgraded = 0, skipped = 0;

        var wants = await index.FindActiveAsync(cmd.UserId, null, UserSkillType.Want, null, ct);
        if (wants.Count == 0) return new GenerateMatchesResult(0, 0, 0);

        var myOffers = await index.FindActiveAsync(cmd.UserId, null, UserSkillType.Offer, null, ct);
        var theirWantsCache = new Dictionary<string, IReadOnlyList<UserSkillIndexEntry>>();

        foreach (var want in wants)
        {
            var offers = await index.FindActiveAsync(null, want.SkillId, UserSkillType.Offer, cmd.UserId, ct);

            foreach (var offer in offers.Where(o => MatchmakingPolicy.IsCompatible(want, o)))
            {
                if (!theirWantsCache.TryGetValue(offer.UserId, out var theirWants))
                    theirWantsCache[offer.UserId] = theirWants =
                        await index.FindActiveAsync(offer.UserId, null, UserSkillType.Want, null, ct);

                // Взаємність: співрозмовник хоче те, що пропоную я.
                var counter = theirWants
                    .SelectMany(w2 => myOffers.Where(o2 => MatchmakingPolicy.IsCompatible(w2, o2)).Select(o2 => (Want: w2, Offer: o2)))
                    .OrderByDescending(pair => MatchmakingPolicy.Score(pair.Want, pair.Offer, mutual: false))
                    .Cast<(UserSkillIndexEntry Want, UserSkillIndexEntry Offer)?>()
                    .FirstOrDefault();

                var mutual = counter is not null;
                var offerRef = new MatchOffer(offer.UserId, cmd.UserId, offer.SkillId, offer.SkillName, offer.Id);
                var counterRef = counter is { } c
                    ? new MatchOffer(cmd.UserId, offer.UserId, c.Offer.SkillId, c.Offer.SkillName, c.Offer.Id)
                    : null;
                var score = MatchmakingPolicy.Score(want, offer, mutual);

                var existing = await matches.GetByOfferKeyAsync(offerRef.FromUserId, offerRef.ToUserId, offerRef.SkillId, ct);
                if (existing is not null)
                {
                    if (mutual && existing.Kind == MatchKind.OneWay)
                    {
                        existing.UpgradeToMutual(counterRef!, score);
                        await matches.UpdateAsync(existing, ct);
                        upgraded++;
                    }
                    else skipped++;
                    continue;
                }

                // Дзеркальна взаємна пара (з протилежним напрямком) уже представляє цю саму угоду.
                if (counterRef is not null)
                {
                    var mirror = await matches.GetByOfferKeyAsync(
                        counterRef.FromUserId, counterRef.ToUserId, counterRef.SkillId, ct);
                    if (mirror is { Kind: MatchKind.Mutual }) { skipped++; continue; }
                }

                var languages = MatchmakingPolicy.CommonLanguages(want, offer);
                await matches.AddAsync(counterRef is null
                    ? Match.OneWay(offerRef, score, languages)
                    : Match.Mutual(offerRef, counterRef, score, languages), ct);
                created++;
            }
        }

        logger.LogInformation("Matches for user {UserId}: created {Created}, upgraded {Upgraded}, skipped {Skipped}",
            cmd.UserId, created, upgraded, skipped);
        return new GenerateMatchesResult(created, upgraded, skipped);
    }
}
