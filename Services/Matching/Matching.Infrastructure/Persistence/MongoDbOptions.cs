using System.ComponentModel.DataAnnotations;

namespace Matching.Infrastructure.Persistence;

/// <summary>
/// Секція "MongoDb" в appsettings.{Environment}.json. Параметри пулу з'єднань і таймаути
/// задаються в рядку підключення (maxPoolSize, serverSelectionTimeoutMS).
/// </summary>
public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    [Required] public string ConnectionString { get; set; } = "";
    [Required] public string DatabaseName { get; set; } = "";
}
