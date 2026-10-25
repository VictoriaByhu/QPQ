using MongoDB.Bson;
using MongoDB.Driver;
using Messages.Domain.Entities;
using Messages.Domain.Interfaces;

namespace Messages.Infrastructure.Persistence.Repositories;

public sealed class AttachmentRepository(MongoDbContext context) : IAttachmentRepository
{
    private readonly IMongoCollection<Attachment> _attachments = context.Attachments;

    public async Task<IReadOnlyList<Attachment>> GetByIdsAsync(
        ObjectId conversationId, IReadOnlyCollection<ObjectId> ids, CancellationToken ct = default)
        => await _attachments.Find(InConversation(conversationId, ids)).ToListAsync(ct);

    public Task<long> CountInConversationAsync(
        ObjectId conversationId, IReadOnlyCollection<ObjectId> ids, CancellationToken ct = default)
        => _attachments.CountDocumentsAsync(InConversation(conversationId, ids), cancellationToken: ct);

    public Task AddAsync(Attachment attachment, CancellationToken ct = default)
        => _attachments.InsertOneAsync(attachment, cancellationToken: ct);

    public Task DeleteByConversationAsync(ObjectId conversationId, CancellationToken ct = default)
        => _attachments.DeleteManyAsync(a => a.ConversationId == conversationId, ct);

    private static FilterDefinition<Attachment> InConversation(ObjectId conversationId, IEnumerable<ObjectId> ids)
        => Builders<Attachment>.Filter.Eq(a => a.ConversationId, conversationId)
           & Builders<Attachment>.Filter.In(a => a.Id, ids);
}
