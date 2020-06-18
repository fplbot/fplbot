using System.Threading.Tasks;
using Dork.Abstractions;
using Dork.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dork.Middlewares
{
    public class Uninstall
    {
        private readonly ILogger<Events> _logger;
        private readonly ISlackTeamRepository _slackTeamRepository;

        public Uninstall(RequestDelegate next, ILogger<Events> logger, ISlackTeamRepository teamRepository)
        {
            _logger = logger;
            _slackTeamRepository = teamRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var metadata = context.Items[HttpItemKeys.EventMetadataKey] as EventMetaData;
            await _slackTeamRepository.DeleteByTeamId(metadata.Team_Id);
            _logger.LogInformation($"Deleted team with TeamId: `{metadata.Team_Id}`");
            context.Response.StatusCode = 200;
        }

        public static bool ShouldRun(HttpContext ctx)
        {
            return ctx.Items.ContainsKey(HttpItemKeys.EventTypeKey) &&
                   (ctx.Items[HttpItemKeys.EventTypeKey].ToString() == EventTypes.AppUninstalled ||
                    ctx.Items[HttpItemKeys.EventTypeKey].ToString() == EventTypes.TokensRevoked);
        }
    }
}