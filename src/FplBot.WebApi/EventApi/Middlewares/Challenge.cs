using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public class Challenge
    {
        private readonly ILogger<Events> _logger;
        public Challenge(RequestDelegate next, ILogger<Events> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var challenge = context.Items[HttpItemKeys.ChallengeKey];

            _logger.LogInformation($"Handling challenge request. Challenge: {challenge}");
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new {challenge}));
        }

        public static bool ShouldRun(HttpContext ctx)
        {
            return ctx.Items.ContainsKey(HttpItemKeys.ChallengeKey);
        }
    }
}