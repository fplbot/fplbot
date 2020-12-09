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
            string sha = "d1a2281945811e1a54370ba361d2beee4614d8db"; // Enable GitVersion some time :)
            return $"You are running @fplbot " +
                   $"v{Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion} " +
                   $"from <https://github.com/fplbot/fplbot/tree/{sha}|{sha}>";
        }
    }
}