using System;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.NServiceBusHandlers.Events
{
    public class AuditHandler : IHandleMessages<AppInstalled>, IHandleMessages<AppUninstalled>
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILogger<AuditHandler> _logger;

        public AuditHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository teamRepo, ILogger<AuditHandler> logger)
        {
            _publisher = publisher;
            _teamRepo = teamRepo;
            _logger = logger;
        }
        
        public async Task Handle(AppInstalled message, IMessageHandlerContext context)
        {
            await PublishToAuditChannel($"🎉 A new workspace ('{message.TeamName}') installed @fplbot!");
        }

        public async Task Handle(AppUninstalled message, IMessageHandlerContext context)
        {
            await PublishToAuditChannel($"😔 '{message.TeamName}' decided to uninstall @fplbot");
        }

        private async Task PublishToAuditChannel(string message)
        {
            _logger.LogInformation(message);
            try
            {
                var blankTeam = await _teamRepo.GetTeam("T0A9QSU83");
                await _publisher.PublishToWorkspace("T0A9QSU83", "#fplbot-notifications", message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }
}