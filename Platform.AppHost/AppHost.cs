using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

// ── Бази даних ───────────────────────────────────────────────────────────────────────────────
// За замовчуванням усе працює локально: SQL Server і MongoDB піднімаються в контейнерах (WithDataVolume —
// дані зберігаються між перезапусками), нічого налаштовувати не потрібно, окрім пароля SQL Server.
// Щоб перемкнути КОНКРЕТНУ базу на віддалений сервер, задайте її рядок підключення в User Secrets AppHost:
//   dotnet user-secrets set "ConnectionStrings:UserSkillsDb" "<рядок підключення>" --project Platform.AppHost
//   (так само CatalogDb і MatchingDb). Без секрету використовується локальний контейнер.
bool IsExternal(string name) => !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(name));

var localSqlNeeded = !IsExternal("UserSkillsDb") || !IsExternal("CatalogDb");
IResourceBuilder<SqlServerServerResource>? sql = null;
if (localSqlNeeded)
{
    // Пароль SQL Server Aspire генерує сам під час першого запуску й зберігає в User Secrets AppHost на цій машині
    // (Parameters:sql-password) — клонувати й запускати можна без жодних налаштувань. Власний пароль можна задати там само.
    var sqlPassword = builder.AddParameter("sql-password",
        new GenerateParameterDefault { MinLength = 20, Special = false }, secret: true, persist: true);
    sql = builder.AddSqlServer("sql", sqlPassword).WithDataVolume();
}

// Ім'я ресурсу = ім'я рядка підключення в сервісі (ConnectionStrings:<ім'я>), друге значення — ім'я бази на сервері.
IResourceBuilder<IResourceWithConnectionString> userSkillsDb;                 // Лаб. 02: ADO.NET + Dapper
if (IsExternal("UserSkillsDb"))
{
    userSkillsDb = builder.AddConnectionString("UserSkillsDb");               // віддалений сервер: схема вже розгорнута (db/p1)
}
else
{
    var local = sql!.AddDatabase("UserSkillsDb", "QPQ_UserSkills");
    userSkillsDb = local;

    // UserSkills працює з T-SQL-схемою та збереженими процедурами (db/p1) — застосовуємо їх, щойно база створена.
    var scriptsDir = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "db", "p1"));
    builder.Eventing.Subscribe<ResourceReadyEvent>(local.Resource, async (e, ct) =>
    {
        var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(local.Resource);
        var connectionString = await local.Resource.ConnectionStringExpression.GetValueAsync(ct)
            ?? throw new InvalidOperationException("UserSkillsDb connection string is not available");

        await SqlScriptRunner.RunAsync(connectionString,
            new[] { "userskills.schema.sql", "userskills.procedures.sql", "userskills.seed.sql" }
                .Select(f => Path.Combine(scriptsDir, f)), logger, ct);
    });
}

IResourceBuilder<IResourceWithConnectionString> catalogDb = IsExternal("CatalogDb")   // Лаб. 03: EF Core
    ? builder.AddConnectionString("CatalogDb")                                         // міграції застосує сам Catalog.Api
    : sql!.AddDatabase("CatalogDb", "QPQ_CatalogEf");

IResourceBuilder<IResourceWithConnectionString> matchingDb = IsExternal("MatchingDb") // Лаб. 04: MongoDB
    ? builder.AddConnectionString("MatchingDb")
    : builder.AddMongoDB("mongo").WithDataVolume().AddDatabase("MatchingDb", "QPQ_Matching");

// ── Redis (Лаб. 06): розподілений кеш, спільний для всіх екземплярів сервісів ─────────────────
// Окремий контейнер; WithDataVolume зберігає кеш між перезапусками. Рядок підключення Aspire передає
// сервісам як ConnectionStrings:redis — в appsettings адресу Redis прописувати не потрібно.
var redis = builder.AddRedis("redis").WithDataVolume();

// ── Мікросервіси з Лаб. 02–04: WithReference(БД) — порти й рядки підключення призначає Aspire ──
// Лаб. 06: кожен сервіс має ще й окремий gRPC-endpoint "grpc" (лише HTTP/2). Без TLS HTTP/2 не узгоджується
// через ALPN, тому REST (HTTP/1.1) і gRPC (h2c) слухають різні порти; порт gRPC сервіс бере зі змінної GRPC_PORT.
var userSkills = builder.AddProject<Projects.UserSkills_Api>("userskills-api")
    .WithReference(userSkillsDb).WaitFor(userSkillsDb)
    .WithHttpEndpoint(name: "grpc", env: "GRPC_PORT")
    .WithHttpHealthCheck("/health");

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb).WaitFor(catalogDb)
    .WithReference(redis).WaitFor(redis)
    .WithHttpEndpoint(name: "grpc", env: "GRPC_PORT")
    .WithHttpHealthCheck("/health");

var matching = builder.AddProject<Projects.Matching_WebApi>("matching-api")
    .WithReference(matchingDb).WaitFor(matchingDb)
    .WithEnvironment("Database__Seed", IsExternal("MatchingDb") ? "false" : "true")   // демо-дані лише для локальної бази
    .WithHttpEndpoint(name: "grpc", env: "GRPC_PORT")
    .WithHttpHealthCheck("/health");

// ── Aggregator → Gateway: порядок запуску БД → сервіси → Aggregator → Gateway задається ланцюжками WaitFor ──
// Aggregator викликає сервіси через gRPC (endpoint "grpc"), кешує зібрані відповіді в Redis; для зовнішніх клієнтів лишається HTTP.
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithHttpEndpoint()                                                   // порт призначає Aspire; знаходиться за іменем "aggregator"
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(userSkills).WithReference(catalog).WithReference(matching)
    .WithReference(redis)
    .WaitFor(userSkills).WaitFor(catalog).WaitFor(matching).WaitFor(redis)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")                         // єдина точка входу для клієнтів
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithExternalHttpEndpoints()
    .WithReference(userSkills).WithReference(catalog).WithReference(matching)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpHealthCheck("/health", endpointName: "public");

builder.Build().Run();
