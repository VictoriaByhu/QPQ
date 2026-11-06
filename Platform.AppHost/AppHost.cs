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

var swaps = builder.AddProject<Projects.Swaps_Api>("swaps-api")
    .WithReference(swapsDb).WaitFor(swapsDb);

var sessions = builder.AddProject<Projects.Sessions_Api>("sessions-api")
    .WithReference(sessionsDb).WaitFor(sessionsDb);

var messages = builder.AddProject<Projects.Messages_WebApi>("messages-api")
    .WithReference(messagesDb).WaitFor(messagesDb);

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(swaps).WithReference(sessions).WithReference(messages)
    .WaitFor(swaps).WaitFor(sessions).WaitFor(messages);

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(swaps).WithReference(sessions).WithReference(messages)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();
