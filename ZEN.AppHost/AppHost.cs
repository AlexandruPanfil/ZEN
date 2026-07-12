var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithDataVolume();

var sql = builder.AddSqlServer("sql")
    .WithImageTag("2019-latest");
    //.WithDataVolume();
var zendb = sql.AddDatabase("zendb");

//var jwtKeyParam = builder.AddParameter("jwt-key", secret: true);
var apiService = builder.AddProject<Projects.ZEN_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(zendb)
    .WaitFor(zendb);
    //.WithEnvironment("Jwt__Key", jwtKeyParam);

builder.AddProject<Projects.ZEN_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(zendb)
    .WaitFor(zendb);

builder.Build().Run();
