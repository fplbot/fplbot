using System.Text.Json;

namespace Discord.Net.Endpoints.Middleware;

internal class PingMiddleware(RequestDelegate next, ILogger<PingMiddleware> logger)
{
    private readonly ILogger<PingMiddleware> _logger = logger;

    public async Task Invoke(HttpContext context)
    {
        _logger.LogInformation("Ping received, responding with Pong");
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { type = 1}));
    }
}
