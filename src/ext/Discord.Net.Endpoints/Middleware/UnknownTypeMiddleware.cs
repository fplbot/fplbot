using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Discord.Net.Endpoints.Middleware
{
    internal class UnknownTypeMiddleware
    {
        private readonly ILogger<UnknownTypeMiddleware> _logger;
        public UnknownTypeMiddleware(RequestDelegate next, ILogger<UnknownTypeMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var type = context.Items[HttpItemKeys.UnhandledKey];
            var payload = context.Items[HttpItemKeys.RawBody];
            _logger.LogWarning($"Not handling. Unsupported payload type `{type}`.\nPayload:\n{payload}");
            var resp = new
            {
                type = 4,
                data = new
                {
                    content = "Received an event I cannot handle"
                }
            };
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(resp);
        }
    }
}
