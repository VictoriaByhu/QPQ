using MongoDB.Bson;
using Messages.Domain.Entities;

namespace Messages.Domain.Interfaces;

public interface IAttachmentRepository
{
    Task<IReadOnlyList<Attachment>> GetByIdsAsync(
        ObjectId conversationId, IReadOnlyCollection<ObjectId> ids, CancellationToken ct = default);
    Task<long> CountInConversationAsync(
        ObjectId conversationId, IReadOnlyCollection<ObjectId> ids, CancellationToken ct = default);
    Task AddAsync(Attachment attachment, CancellationToken ct = default);
    Task DeleteByConversationAsync(ObjectId conversationId, CancellationToken ct = default);
}
