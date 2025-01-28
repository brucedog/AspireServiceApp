var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.AspireServiceApp_ApiService>("apiservice");
var dockerService = builder.AddProject<Projects.AspireServiceApp_DockerService>("dockerservice");

builder.AddProject<Projects.AspireServiceApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(dockerService)
    .WaitFor(dockerService);


builder.Build().Run();
