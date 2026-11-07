var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var postgres = builder.AddPostgres("postgres");
var laboratorio = postgres.AddDatabase("laboratorio");

builder.AddProject<Projects.Laboratorio_Api>("laboratorio-api")
    .WithReference(redis)
    .WithReference(laboratorio)
    .WaitFor(redis)
    .WaitFor(laboratorio)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
