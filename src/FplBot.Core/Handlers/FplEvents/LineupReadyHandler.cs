using System;
using System.Threading.Tasks;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    public class LineupReadyHandler : IHandleMessages<LineupReady>, IHandleMessages<PublishLineupsToSlackWorkspace>
    {
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ISlackClientBuilder _builder;
        private readonly ILogger<LineupReadyHandler> _logger;

        public LineupReadyHandler(ISlackTeamRepository slackTeamRepo, ISlackClientBuilder builder, ILogger<LineupReadyHandler> logger)
        {
            _slackTeamRepo = slackTeamRepo;
            _builder = builder;
            _logger = logger;
        }

        public async Task Handle(LineupReady message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Handling new lineups");
            var slackTeams = await _slackTeamRepo.GetAllTeams();

            foreach (var slackTeam in slackTeams)
            {
                if (slackTeam.Subscriptions.ContainsSubscriptionFor(EventSubscription.Lineups))
                {
                    var command = new PublishLineupsToSlackWorkspace(slackTeam.TeamId, message.Lineup);
                    await context.SendLocal(command);
                }
            }
        }

        public async Task Handle(PublishLineupsToSlackWorkspace message, IMessageHandlerContext context)
        {
            var team = await _slackTeamRepo.GetTeam(message.WorkspaceId);
            var slackClient = _builder.Build(team.AccessToken);
            var lineups = message.Lineups;
            var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* 👇";

            var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, firstMessage);
            if (res.Ok)
            {
                var formattedLineup = Formatter.FormatLineup(lineups);
                await context.SendLocal(new PublishSlackThreadMessage
                (
                    message.WorkspaceId,
                    team.FplBotSlackChannel,
                    res.ts,
                    formattedLineup
                ));
            }
        }
    }
}
