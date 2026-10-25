var builder = DistributedApplication.CreateBuilder(args);

var swapsDb = builder.AddConnectionString("SwapsDb");

builder.AddProject<Projects.Swaps_Api>("swaps-api")
    .WithReference(swapsDb);

var sessionsDb = builder.AddConnectionString("SessionsDb");

builder.AddProject<Projects.Sessions_Api>("sessions-api")
    .WithReference(sessionsDb);

var messagesDb = builder.AddConnectionString("MessagesDb");

builder.AddProject<Projects.Messages_WebApi>("messages-api")
    .WithReference(messagesDb);

builder.Build().Run();
