using MongoDB.Bson;
using Messages.Domain.Entities;

namespace Messages.Domain.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task<bool> IsParticipantAsync(ObjectId id, string userId, CancellationToken ct = default);
    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task UpdateAsync(Conversation conversation, CancellationToken ct = default);
    Task DeleteAsync(Conversation conversation, CancellationToken ct = default);
}
