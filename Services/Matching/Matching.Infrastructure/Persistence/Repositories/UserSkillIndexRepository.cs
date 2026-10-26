using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Interfaces;

namespace Matching.Infrastructure.Persistence.Repositories;

public sealed class UserSkillIndexRepository(MongoDbContext context) : IUserSkillIndexRepository
{
    private readonly IMongoCollection<UserSkillIndexEntry> _entries = context.UserSkillIndex;

    public async Task<UserSkillIndexEntry?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _entries.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public Task<bool> ExistsByUserSkillIdAsync(int userSkillId, CancellationToken ct = default)
        => _entries.Find(x => x.UserSkillId == userSkillId).AnyAsync(ct);

    public async Task<IReadOnlyList<UserSkillIndexEntry>> FindActiveAsync(
        string? userId, int? skillId, UserSkillType? type, string? excludeUserId, CancellationToken ct = default)
    {
        var f = Builders<UserSkillIndexEntry>.Filter;
        var filter = f.Eq(x => x.Status, UserSkillStatus.Active);
        if (userId is not null) filter &= f.Eq(x => x.UserId, userId);
        if (excludeUserId is not null) filter &= f.Ne(x => x.UserId, excludeUserId);
        if (skillId is not null) filter &= f.Eq(x => x.SkillId, skillId.Value);
        if (type is not null) filter &= f.Eq(x => x.Type, type.Value);

        return await _entries.Find(filter).SortBy(x => x.Id).ToListAsync(ct);
    }

    public Task AddAsync(UserSkillIndexEntry entry, CancellationToken ct = default)
        => _entries.InsertOneAsync(entry, cancellationToken: ct);

    public Task UpdateAsync(UserSkillIndexEntry entry, CancellationToken ct = default)
        => VersionedWrites.ReplaceAsync(_entries, entry, "Index entry", ct);

    public async Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default)
        => (await _entries.DeleteOneAsync(x => x.Id == id, ct)).DeletedCount > 0;
}
