using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Discord.Net.Endpoints.Middleware;

internal class HttpItemsManager
{
    private readonly RequestDelegate _next;
    private ILogger<HttpItemsManager> _logger;

    public HttpItemsManager(RequestDelegate next, ILogger<HttpItemsManager> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        if (body.StartsWith("{"))
        {
            var jObject = JsonDocument.Parse(body);
            _logger.LogTrace(body);
            bool hasType = jObject.RootElement.TryGetProperty("type", out JsonElement typeValue);
            if (hasType)
            {
                int typeValueAsInt = typeValue.GetInt32();
                var kvp = typeValueAsInt switch
                {
                    1 => new KeyValuePair<object, object>(HttpItemKeys.PingKey, typeValueAsInt),
                    2 => new KeyValuePair<object, object>(HttpItemKeys.SlashCommandsKey, jObject),
                    _ => new KeyValuePair<object, object>(HttpItemKeys.UnhandledKey, typeValueAsInt)
                };
                context.Items.Add(HttpItemKeys.TypeKey, typeValueAsInt);
                context.Items.Add(kvp);
                context.Items.Add(HttpItemKeys.RawBody, body);
            }
        }
        context.Request.Body.Position = 0;

        await _next(context);
    }
}