using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using Messages.Domain.Entities;

namespace Messages.Infrastructure.Persistence;

public sealed class MongoDbContext
{
    public const string ConversationsCollection = "conversations";
    public const string MessagesCollection = "messages";
    public const string AttachmentsCollection = "attachments";

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;
        var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        var client = new MongoClient(clientSettings);
        Database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<Conversation> Conversations
        => Database.GetCollection<Conversation>(ConversationsCollection);

    public IMongoCollection<Message> Messages
        => Database.GetCollection<Message>(MessagesCollection);

    public IMongoCollection<Attachment> Attachments
        => Database.GetCollection<Attachment>(AttachmentsCollection);
}
