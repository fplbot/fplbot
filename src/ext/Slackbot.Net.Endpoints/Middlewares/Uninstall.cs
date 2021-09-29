using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints.Middlewares
{
    public class Uninstall
    {
        private readonly ILogger<Uninstall> _logger;
        private readonly IUninstall _uninstaller;
        private readonly ITokenStore _tokenStore;

        public Uninstall(RequestDelegate next, ILogger<Uninstall> logger, IServiceProvider provider)
        {
            _logger = logger;
            _uninstaller = provider.GetService<IUninstall>() ?? new NoopUninstaller(provider.GetService<ILogger<NoopUninstaller>>());
            _tokenStore = provider.GetService<ITokenStore>();
        }

        public async Task Invoke(HttpContext context)
        {
            var metadata = context.Items[HttpItemKeys.EventMetadataKey] as EventMetaData;
            var deleted = await _tokenStore.Delete(metadata.Team_Id);
            await _uninstaller.OnUninstalled(deleted.TeamId, deleted.TeamName);
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
        public Task OnUninstalled(string teamId, string teamName)
        {
            _logger.LogDebug("No OnUninstall function registered. No-op.");
            return Task.CompletedTask;
        }
    }
}
