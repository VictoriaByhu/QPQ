var builder = DistributedApplication.CreateBuilder(args);

var swapsScript = string.Join(
    Environment.NewLine,
    new[] { "schema.sql", "procedures.sql", "seed.sql" }
        .Select(file => File.ReadAllText(Path.Combine(builder.AppHostDirectory, "..", "db", "p1", file)).TrimEnd())
        .Select(sql => sql.EndsWith("GO", StringComparison.OrdinalIgnoreCase) ? sql : sql + Environment.NewLine + "GO"));

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword).WithDataVolume();
var swapsDb = sql.AddDatabase("SwapsDb", "QPQ_Swaps").WithCreationScript(swapsScript);
var sessionsDb = sql.AddDatabase("SessionsDb", "QPQ_SessionsEf");

var mongo = builder.AddMongoDB("mongo").WithDataVolume();
var messagesDb = mongo.AddDatabase("MessagesDb", "QPQ_Messages");

var redis = builder.AddRedis("redis").WithDataVolume();

// Swap events: swaps-api publishes, sessions-api and messages-api keep their copies in sync.
var rabbit = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin();

// WithHttpHealthCheck makes the dashboard show real health and makes WaitFor wait for Healthy, not just a started process.
// /health answers 200 for Healthy and Degraded, so a stopped Redis does not hold back the services that depend on it.
var swaps = builder.AddProject<Projects.Swaps_Api>("swaps-api")
    .WithReference(swapsDb).WaitFor(swapsDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpHealthCheck("/health");

var sessions = builder.AddProject<Projects.Sessions_Api>("sessions-api")
    .WithReference(sessionsDb).WaitFor(sessionsDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpHealthCheck("/health");

var messages = builder.AddProject<Projects.Messages_WebApi>("messages-api")
    .WithReference(messagesDb).WaitFor(messagesDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpHealthCheck("/health");

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(swaps).WithReference(sessions).WithReference(messages)
    .WithReference(redis).WaitFor(redis)
    .WaitFor(swaps).WaitFor(sessions).WaitFor(messages)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(swaps).WithReference(sessions).WithReference(messages)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
