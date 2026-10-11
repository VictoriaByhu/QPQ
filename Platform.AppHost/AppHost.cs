var builder = DistributedApplication.CreateBuilder(args);

var swapsDb = builder.AddConnectionString("SwapsDb");

builder.AddProject<Projects.Swaps_Api>("swaps-api")
    .WithReference(swapsDb);

builder.Build().Run();
