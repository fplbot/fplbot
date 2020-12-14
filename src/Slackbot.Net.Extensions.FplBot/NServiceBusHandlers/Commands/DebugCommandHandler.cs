using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.NServiceBusHandlers.Commands
{
    public class DebugCommandHandler : IHandleMessages<ReplyDebugInfo>
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILogger<DebugCommandHandler> _logger;

        public DebugCommandHandler(ISlackTeamRepository teamRepo, ISlackWorkSpacePublisher publisher, ILogger<DebugCommandHandler> logger)
        {
            _teamRepo = teamRepo;
            _publisher = publisher;
            _logger = logger;
        }
        
        public async Task Handle(ReplyDebugInfo message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Debugging!");
            await _publisher.PublishToWorkspace(message.TeamId, message.Channel, DebugInfo());
        }

        private string DebugInfo()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            Version version = entryAssembly?.GetName()?.Version;
            string versionStr = $"{version?.Major}.{version?.Minor}.{version?.Build}";
            string informationalVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var sha = informationalVersion?.Split(".").Last();
            return $"ℹ️ I'm currently at v{versionStr}\n" +
                   $"▪️ Full version: {informationalVersion}\n" +
                   $"▪️ <https://github.com/fplbot/fplbot/releases/tag/{versionStr}|Release notes>\n" +
                   $"️▪️ <https://github.com/fplbot/fplbot/tree/{sha}|{sha?.Substring(0,sha.Length-1)}>";
        }
    }
}