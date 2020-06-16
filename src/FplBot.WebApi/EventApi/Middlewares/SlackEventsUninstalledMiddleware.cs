using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public class SlackEventsUninstalledMiddleware
    {
        private readonly ILogger<SlackEventsMiddleware> _logger;
        private readonly ISlackTeamRepository _slackTeamRepository;

        public SlackEventsUninstalledMiddleware(ILogger<SlackEventsMiddleware> logger, ISlackTeamRepository teamRepository)
        {
            _logger = logger;
            _slackTeamRepository = teamRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var body = context.Request.Form;
            var teamId = body["team_id"];
            await _slackTeamRepository.DeleteByTeamId(teamId);
            _logger.LogInformation($"Deleted team with TeamId: `{teamId}`");
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
        }

        public static bool ShouldRun(HttpContext ctx, string path)
        {
            var isPostForPath = ctx.Request.Method == "POST" && ctx.Request.Path == path;
            var jObject = JObject.Parse(ctx.Request.Form["event"]);
            var eventType = jObject["type"].Value<string>();
            bool isUninstallEvent = eventType == EventTypes.AppUninstalled || eventType == EventTypes.TokensRevoked;
            return isPostForPath && ctx.Request.Form.ContainsKey("event") && isUninstallEvent;
        }
    }
}