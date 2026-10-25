using MongoDB.Bson;
using Messages.Domain.Entities;

namespace Messages.Domain.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task<Message?> GetLatestVisibleAsync(ObjectId conversationId, CancellationToken ct = default);
    Task AddAsync(Message message, CancellationToken ct = default);
    Task UpdateAsync(Message message, CancellationToken ct = default);
    Task DeleteByConversationAsync(ObjectId conversationId, CancellationToken ct = default);
}
