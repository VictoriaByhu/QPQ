namespace QPQ.Reviews.Infrastructure.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ReviewsCollectionName { get; set; } = string.Empty;
}