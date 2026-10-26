using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Domain.Entities;
using Matching.Domain.Interfaces;

namespace Matching.Infrastructure.Persistence.Repositories;

public sealed class MatchRepository(MongoDbContext context) : IMatchRepository
{
    private readonly IMongoCollection<Match> _matches = context.Matches;

    public async Task<Match?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _matches.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<Match?> GetByOfferKeyAsync(
        string fromUserId, string toUserId, int skillId, CancellationToken ct = default)
        => await _matches.Find(x => x.Offer.FromUserId == fromUserId
                                    && x.Offer.ToUserId == toUserId
                                    && x.Offer.SkillId == skillId)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(Match match, CancellationToken ct = default)
        => _matches.InsertOneAsync(match, cancellationToken: ct);

    public Task UpdateAsync(Match match, CancellationToken ct = default)
        => VersionedWrites.ReplaceAsync(_matches, match, "Match", ct);
}
