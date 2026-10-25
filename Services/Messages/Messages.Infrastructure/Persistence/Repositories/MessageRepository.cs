using MongoDB.Bson;
using MongoDB.Driver;
using Messages.Domain.Constants;
using Messages.Domain.Entities;
using Messages.Domain.Interfaces;

namespace Messages.Infrastructure.Persistence.Repositories;

public sealed class MessageRepository(MongoDbContext context) : IMessageRepository
{
    private readonly IMongoCollection<Message> _messages = context.Messages;

    public async Task<Message?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _messages.Find(m => m.Id == id).FirstOrDefaultAsync(ct);

    public async Task<Message?> GetLatestVisibleAsync(ObjectId conversationId, CancellationToken ct = default)
    {
        var filter = Builders<Message>.Filter;
        return await _messages
            .Find(filter.Eq(m => m.ConversationId, conversationId)
                  & filter.Eq(m => m.IsDeleted, false)
                  & filter.Ne(MongoMappings.DiscriminatorElement, MessageTypes.System))
            .SortByDescending(m => m.CreatedAt)
            .Limit(1)
            .FirstOrDefaultAsync(ct);
    }

    public Task AddAsync(Message message, CancellationToken ct = default)
        => _messages.InsertOneAsync(message, cancellationToken: ct);

    public Task UpdateAsync(Message message, CancellationToken ct = default)
        => _messages.ReplaceWithVersionAsync(message, ct);

    public Task DeleteByConversationAsync(ObjectId conversationId, CancellationToken ct = default)
        => _messages.DeleteManyAsync(m => m.ConversationId == conversationId, ct);
}
