var builder = DistributedApplication.CreateBuilder(args);

// Секретний пароль (ми його вже додали в User Secrets)
var sqlPassword = builder.AddParameter("sql-password", secret: true);

// 1. Бази даних з постійним сховищем (WithDataVolume)
var sql = builder.AddSqlServer("sql", sqlPassword)
                 // .AddDatabase("SomeSqlDb").WithDataVolume("SomeSqlDb") // розкоментуєш, коли додаси SQL-сервіс
                 .WithDataVolume("sql-data");

var mongo = builder.AddMongoDB("mongo")
                   .WithDataVolume("ReviewsDb") // Спочатку чіпляємо том до сервера
                   .AddDatabase("ReviewsDb");   // Потім створюємо БД

// 2. Сервіси (чекають на БД)
var reviewsApi = builder.AddProject<Projects.QPQ_Reviews_WebApi>("reviews-api")
                        .WithReference(mongo)
                        .WaitFor(mongo);

// 3. Агрегатор (чекає на сервіси)
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
                        .WithReference(reviewsApi)
                        .WaitFor(reviewsApi); // Запускається тільки коли API готове

// 4. API Gateway (чекає на агрегатор, має фіксований порт)
var gateway = builder.AddProject<Projects.Platform_ApiGateway>("gateway")
                     .WithReference(reviewsApi)
                     .WithReference(aggregator)
                     .WaitFor(aggregator);  
                     // Задаємо фіксовані порти для зручного тестування
                    // .WithHttpEndpoint(port: 5000, name: "http")
                    // .WithHttpsEndpoint(port: 5001, name: "https");

builder.Build().Run();