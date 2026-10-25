namespace Messages.Infrastructure.Persistence;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";
    public const string ConnectionStringName = "MessagesDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool SeedData { get; set; } = true;
}
