using Microsoft.Extensions.Logging;
using Messages.Infrastructure.Persistence;

namespace Messages.Infrastructure.Seeding;

public sealed class ConversationSeeder(MongoDbContext context, ILogger<ConversationSeeder> logger)
    : JsonDataSeeder(context, logger)
{
    protected override string CollectionName => MongoDbContext.ConversationsCollection;
}

public sealed class AttachmentSeeder(MongoDbContext context, ILogger<AttachmentSeeder> logger)
    : JsonDataSeeder(context, logger)
{
    protected override string CollectionName => MongoDbContext.AttachmentsCollection;
}

public sealed class MessageSeeder(MongoDbContext context, ILogger<MessageSeeder> logger)
    : JsonDataSeeder(context, logger)
{
    protected override string CollectionName => MongoDbContext.MessagesCollection;
}
