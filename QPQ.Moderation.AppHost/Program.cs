var builder = DistributedApplication.CreateBuilder(args);

// Подтягиваем строку подключения
var moderationDb = builder.AddConnectionString("ModerationDb");

// Регистрируем наш API и передаем ему ссылку на базу данных
builder.AddProject<Projects.QPQ_Moderation_Api>("moderation-api")
    .WithReference(moderationDb);

builder.Build().Run();