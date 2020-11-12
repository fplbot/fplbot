using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints.Middlewares
{
    public class Uninstall
    {
        private readonly ILogger<Uninstall> _logger;
        private readonly IUninstall _uninstaller;

        public Uninstall(RequestDelegate next, ILogger<Uninstall> logger, IServiceProvider provider)
        {
            _logger = logger;
            _uninstaller = provider.GetService<IUninstall>() ?? new NoopUninstaller(provider.GetService<ILogger<NoopUninstaller>>());
        }

        public async Task Invoke(HttpContext context)
        {
            var metadata = context.Items[HttpItemKeys.EventMetadataKey] as EventMetaData;
            await _uninstaller.Uninstall(metadata.Team_Id);
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

    public class NoopUninstaller : IUninstall
    {
        private readonly ILogger<NoopUninstaller> _logger;

        public NoopUninstaller(ILogger<NoopUninstaller> logger)
        {
            _logger = logger;
        }
        public Task Uninstall(string metadataTeamId)
        {
            _logger.LogWarning("No uninstall provider registered! No uninstall will be executed");
            return Task.CompletedTask;
        }
    }
}