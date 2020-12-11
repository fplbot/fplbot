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
            string informationalVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var sha = informationalVersion?.Split(".").Last();
            return $"You are running @fplbot " +
                   $"v{informationalVersion} " +
                   $"\n<https://github.com/fplbot/fplbot/tree/{sha}|Browse {sha?.Substring(0,sha.Length-1)} on github.com/fplbot/fplbot>";
        }
    }
}