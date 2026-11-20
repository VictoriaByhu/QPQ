var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);

// 1. Бази даних та Кеш з постійним сховищем
var sql = builder.AddSqlServer("sql", sqlPassword)
                 .WithDataVolume("sql-data");

var mongo = builder.AddMongoDB("mongo")
                   .WithDataVolume("ReviewsDb")
                   .AddDatabase("ReviewsDb");

var redis = builder.AddRedis("redis")
                   .WithDataVolume("redis-data");

// 2. Сервіси (з перевіркою стану /health)
var reviewsApi = builder.AddProject<Projects.QPQ_Reviews_WebApi>("reviews-api")
                        .WithReference(mongo)
                        .WaitFor(mongo)
                        .WithReference(redis)
                        .WaitFor(redis)
                        .WithHttpHealthCheck("/health");

// 3. Агрегатор (чекає на Healthy стан reviews-api та Redis)
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
                        .WithReference(reviewsApi)
                        .WaitFor(reviewsApi)
                        .WithReference(redis)
                        .WaitFor(redis)
                        .WithHttpHealthCheck("/health");

// 4. API Gateway
var gateway = builder.AddProject<Projects.Platform_ApiGateway>("gateway")
                     .WithReference(reviewsApi)
                     .WithReference(aggregator)
                     .WaitFor(aggregator)
                     .WithHttpHealthCheck("/health");

builder.Build().Run();