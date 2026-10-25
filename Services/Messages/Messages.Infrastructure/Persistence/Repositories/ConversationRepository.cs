using MongoDB.Bson;
using MongoDB.Driver;
using Messages.Domain.Entities;
using Messages.Domain.Interfaces;

namespace Messages.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository(MongoDbContext context) : IConversationRepository
{
    private readonly IMongoCollection<Conversation> _conversations = context.Conversations;

    public async Task<Conversation?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _conversations.Find(c => c.Id == id).FirstOrDefaultAsync(ct);

    public Task<bool> IsParticipantAsync(ObjectId id, string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var user))
            return Task.FromResult(false);

        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, id)
                     & Builders<Conversation>.Filter.Eq("participants.userId", user.ToString("D"));
        return _conversations.Find(filter).AnyAsync(ct);
    }

    public Task AddAsync(Conversation conversation, CancellationToken ct = default)
        => _conversations.InsertOneAsync(conversation, cancellationToken: ct);

    public Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
        => _conversations.ReplaceWithVersionAsync(conversation, ct);

    public Task DeleteAsync(Conversation conversation, CancellationToken ct = default)
        => _conversations.DeleteWithVersionAsync(conversation, ct);
}
