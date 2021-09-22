using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    internal class GameweekStartedHandler : IHandleMessages<GameweekJustBegan>, IHandleMessages<ProcessGameweekStartedForSlackWorkspace>
    {
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILogger<GameweekStartedHandler> _logger;

        public GameweekStartedHandler(ICaptainsByGameWeek captainsByGameweek,
            ITransfersByGameWeek transfersByGameweek,
            ISlackWorkSpacePublisher publisher,
            ISlackTeamRepository teamsRepo,
            ILogger<GameweekStartedHandler> logger)
        {
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _logger = logger;
        }

        public async Task Handle(GameweekJustBegan notification, IMessageHandlerContext context)
        {
            var teams = await _teamRepo.GetAllTeams();
            foreach (var team in teams)
            {
                await context.SendLocal(new ProcessGameweekStartedForSlackWorkspace(team.TeamId, notification.NewGameweek.Id));
            }
        }

        public async Task Handle(ProcessGameweekStartedForSlackWorkspace message, IMessageHandlerContext context)
        {
            var newGameweek = message.GameweekId;

            var team = await _teamRepo.GetTeam(message.WorkspaceId);
            if(team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Captains) || team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Transfers))
                await _publisher.PublishToWorkspace($"Gameweek {message.GameweekId}!");

            var messages = new List<string>();

            if (team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Captains))
            {
                messages.Add(await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek, (int)team.FplbotLeagueId));
                messages.Add(await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, (int)team.FplbotLeagueId));
            }
            else
            {
                _logger.LogInformation("Team {team} hasn't subscribed for gw start captains, so bypassing it", team.TeamId);
            }

            if (team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Transfers))
            {
                messages.Add(await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, (int)team.FplbotLeagueId));
            }
            else
            {
                _logger.LogInformation("Team {team} hasn't subscribed for gw start transfers, so bypassing it", team.TeamId);
            }

            await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, messages.ToArray());
        }
    }
}
