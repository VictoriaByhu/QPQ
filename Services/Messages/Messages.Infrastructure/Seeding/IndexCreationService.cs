using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Messages.Domain.Entities;
using Messages.Infrastructure.Persistence;

namespace Messages.Infrastructure.Seeding;

public sealed class IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CreateConversationIndexesAsync(cancellationToken);
            await CreateMessageIndexesAsync(cancellationToken);
            await CreateAttachmentIndexesAsync(cancellationToken);
            logger.LogInformation("MongoDB indexes are up to date");
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            logger.LogError(ex, "Failed to create MongoDB indexes");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private Task CreateConversationIndexesAsync(CancellationToken ct)
    {
        var keys = Builders<Conversation>.IndexKeys;
        return context.Conversations.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Conversation>(
                keys.Ascending(c => c.SwapId),
                new CreateIndexOptions { Name = "ux_conversations_swap", Unique = true }),
            new CreateIndexModel<Conversation>(
                keys.Ascending("participants.userId"),
                new CreateIndexOptions { Name = "ix_conversations_participant" })
        ], ct);
    }

    private Task CreateMessageIndexesAsync(CancellationToken ct)
    {
        var keys = Builders<Message>.IndexKeys;
        return context.Messages.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Message>(
                keys.Ascending(m => m.ConversationId).Ascending(m => m.CreatedAt),
                new CreateIndexOptions<Message>
                {
                    Name = "ix_messages_conversation_createdAt",
                    PartialFilterExpression = Builders<Message>.Filter.Eq(m => m.IsDeleted, false)
                }),
            new CreateIndexModel<Message>(
                keys.Ascending(m => m.SenderId).Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "ix_messages_sender_createdAt" }),
            new CreateIndexModel<Message>(
                keys.Text("text").Text("caption"),
                new CreateIndexOptions { Name = "tx_messages_content", DefaultLanguage = "none" })
        ], ct);
    }

    private Task CreateAttachmentIndexesAsync(CancellationToken ct)
    {
        var keys = Builders<Attachment>.IndexKeys;
        return context.Attachments.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Attachment>(
                keys.Ascending(a => a.ConversationId).Descending(a => a.UploadedAt),
                new CreateIndexOptions { Name = "ix_attachments_conversation_uploadedAt" })
        ], ct);
    }
}
