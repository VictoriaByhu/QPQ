using MongoDB.Bson;
using MongoDB.Driver;
using Messages.Application.Attachments.Dtos;
using Messages.Application.Common.Interfaces;
using Messages.Application.Common.Models;
using Messages.Application.Conversations.Dtos;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Entities;

namespace Messages.Infrastructure.Persistence.Queries;

public sealed class MessageQueries(MongoDbContext context) : IMessageQueries
{
    private readonly IMongoCollection<Conversation> _conversations = context.Conversations;
    private readonly IMongoCollection<Message> _messages = context.Messages;
    private readonly IMongoCollection<Attachment> _attachments = context.Attachments;

    private static readonly BsonDocument ConversationProjection = new()
    {
        { "_id", ToStr("$_id") },
        { "swapId", 1 },
        { "swapStatus", 1 },
        { "participants", 1 },
        {
            "lastMessage", new BsonDocument("$cond", new BsonArray
            {
                new BsonDocument("$eq", new BsonArray { IfNull("$lastMessage", BsonNull.Value), BsonNull.Value }),
                BsonNull.Value,
                new BsonDocument
                {
                    { "messageId", ToStr("$lastMessage.messageId") },
                    { "senderId", "$lastMessage.senderId" },
                    { "preview", "$lastMessage.preview" },
                    { "sentAt", "$lastMessage.sentAt" }
                }
            })
        },
        { "createdAt", 1 },
        { "updatedAt", 1 },
        { "version", IfNull("$version", 0) }
    };

    private static readonly BsonDocument MessageProjection = new()
    {
        { "_id", ToStr("$_id") },
        { "conversationId", ToStr("$conversationId") },
        { "senderId", 1 },
        { "type", 1 },
        { "text", 1 },
        { "editedAt", 1 },
        { "caption", 1 },
        {
            "attachments", new BsonDocument("$map", new BsonDocument
            {
                { "input", IfNull("$attachments", new BsonArray()) },
                { "as", "a" },
                {
                    "in", new BsonDocument
                    {
                        { "attachmentId", ToStr("$$a.attachmentId") },
                        { "fileName", "$$a.fileName" }
                    }
                }
            })
        },
        { "event", 1 },
        { "swapId", "$details.swapId" },
        { "createdAt", 1 },
        { "version", IfNull("$version", 0) }
    };

    private static readonly BsonDocument AttachmentProjection = new()
    {
        { "_id", ToStr("$_id") },
        { "conversationId", ToStr("$conversationId") },
        { "uploadedBy", 1 },
        { "fileName", 1 },
        { "url", 1 },
        { "contentType", 1 },
        { "sizeBytes", 1 },
        { "uploadedAt", 1 }
    };

    public async Task<ConversationDto?> GetConversationAsync(string id, string userId, CancellationToken ct)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.Id, ObjectId.Parse(id))
                     & ParticipantFilter(userId);
        return await _conversations.Aggregate()
            .Match(filter)
            .Project<ConversationDto>(ConversationProjection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<ConversationDto>> GetConversationsAsync(
        string userId, string? swapStatus, int page, int pageSize, CancellationToken ct)
    {
        var filter = ParticipantFilter(userId);
        if (swapStatus is not null)
            filter &= Builders<Conversation>.Filter.Eq("swapStatus", swapStatus);

        var total = await _conversations.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _conversations.Aggregate()
            .Match(filter)
            .Sort(new BsonDocument { { "lastMessage.sentAt", -1 }, { "createdAt", -1 } })
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<ConversationDto>(ConversationProjection)
            .ToListAsync(ct);

        return new PagedResult<ConversationDto>(items, total, page, pageSize);
    }

    public async Task<MessageDto?> GetMessageAsync(string id, CancellationToken ct)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.Id, ObjectId.Parse(id))
                     & Builders<Message>.Filter.Eq(m => m.IsDeleted, false);
        return await _messages.Aggregate()
            .Match(filter)
            .Project<MessageDto>(MessageProjection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<MessageDto>> GetMessagesAsync(
        string conversationId, string? search, string? senderId, int page, int pageSize, CancellationToken ct)
    {
        var builder = Builders<Message>.Filter;
        var filter = builder.Eq(m => m.ConversationId, ObjectId.Parse(conversationId))
                     & builder.Eq(m => m.IsDeleted, false);
        if (senderId is not null)
            filter &= builder.Eq(m => m.SenderId, senderId);
        if (search is not null)
            filter &= builder.Text(search, new TextSearchOptions { CaseSensitive = false });

        var total = await _messages.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _messages.Aggregate()
            .Match(filter)
            .Sort(new BsonDocument { { "createdAt", 1 }, { "_id", 1 } })
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<MessageDto>(MessageProjection)
            .ToListAsync(ct);

        return new PagedResult<MessageDto>(items, total, page, pageSize);
    }

    public async Task<AttachmentDto?> GetAttachmentAsync(string id, CancellationToken ct)
        => await _attachments.Aggregate()
            .Match(Builders<Attachment>.Filter.Eq(a => a.Id, ObjectId.Parse(id)))
            .Project<AttachmentDto>(AttachmentProjection)
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<AttachmentDto>> GetAttachmentsAsync(
        string conversationId, int page, int pageSize, CancellationToken ct)
    {
        var filter = Builders<Attachment>.Filter.Eq(a => a.ConversationId, ObjectId.Parse(conversationId));

        var total = await _attachments.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _attachments.Aggregate()
            .Match(filter)
            .Sort(new BsonDocument { { "uploadedAt", -1 }, { "_id", -1 } })
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<AttachmentDto>(AttachmentProjection)
            .ToListAsync(ct);

        return new PagedResult<AttachmentDto>(items, total, page, pageSize);
    }

    private static FilterDefinition<Conversation> ParticipantFilter(string userId)
        => Builders<Conversation>.Filter.Eq("participants.userId", Guid.Parse(userId).ToString("D"));

    private static BsonDocument ToStr(string field) => new("$toString", field);

    private static BsonDocument IfNull(string field, BsonValue fallback) => new("$ifNull", new BsonArray { field, fallback });
}
