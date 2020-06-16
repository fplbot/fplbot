using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public class SlackEventsChallengeMiddleware
    {
        private readonly ILogger<SlackEventsMiddleware> _logger;
        public SlackEventsChallengeMiddleware(ILogger<SlackEventsMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            
            var body = context.Request.Form;
            string challenge = body["challenge"];
            _logger.LogInformation($"Handling challenge request. Challenge: {challenge}");
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new {challenge}));
        }

        public static bool ShouldRun(HttpContext ctx, string path)
        {
            var isPostForPath = ctx.Request.Method == "POST" && ctx.Request.Path == path;
            return isPostForPath && ctx.Request.Form.ContainsKey("challenge");
        }
    }
}