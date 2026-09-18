using System.Text.Json;

namespace Discord.Net.Endpoints.Middleware;

#pragma warning disable CS9113
internal class PingMiddleware(RequestDelegate next, ILogger<PingMiddleware> logger)
#pragma warning restore CS9113
{
    private readonly ILogger<PingMiddleware> _logger = logger;

    public async Task Invoke(HttpContext context)
    {
        _logger.LogInformation("Ping received, responding with Pong");
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { type = 1 }));
    }
}
