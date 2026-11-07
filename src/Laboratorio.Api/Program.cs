using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("redis");
builder.AddNpgsqlDataSource("laboratorio");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "Laboratorio.Api",
    status = "running"
}));

app.MapGet("/status", async (
    IConnectionMultiplexer redis,
    NpgsqlDataSource dataSource,
    CancellationToken cancellationToken) =>
{
    var latency = await redis.GetDatabase().PingAsync();

    await using var command = dataSource.CreateCommand("SELECT 1");
    var scalar = await command.ExecuteScalarAsync(cancellationToken);
    var postgresReady = scalar is not null && Convert.ToInt32(scalar) == 1;

    if (!postgresReady)
    {
        return Results.Json(new
        {
            service = "Laboratorio.Api",
            redis = "PONG",
            postgres = "not-ready"
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        service = "Laboratorio.Api",
        redis = "PONG",
        redisLatencyMs = latency.TotalMilliseconds,
        postgres = "ready"
    });
});

app.Run();
