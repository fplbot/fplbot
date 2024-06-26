using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Handlers;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    internal class GameweekEndedHandler : IHandleMessages<GameweekFinished>, IHandleMessages<PublishStandingsToSlackWorkspace>
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILeagueClient _leagueClient;
        private readonly IGlobalSettingsClient _gameweekClient;
        private readonly ISlackTeamRepository _teamRepo;

        public GameweekEndedHandler(ISlackWorkSpacePublisher publisher,
            ISlackTeamRepository teamsRepo,
            ILeagueClient leagueClient,
            IGlobalSettingsClient gameweekClient)
        {
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _leagueClient = leagueClient;
            _gameweekClient = gameweekClient;
        }

        public async Task Handle(GameweekFinished notification, IMessageHandlerContext context)
        {
            var teams = await _teamRepo.GetAllTeams();
            foreach (var team in teams)
            {
                if (team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Standings))
                {
                    await context.SendLocal(new PublishStandingsToSlackWorkspace(team.TeamId, team.FplBotSlackChannel, (int)team.FplbotLeagueId, notification.FinishedGameweek.Id));
                }
            }
        }

        public async Task Handle(PublishStandingsToSlackWorkspace message, IMessageHandlerContext context)
        {
            var settings = await _gameweekClient.GetGlobalSettings();
            var gameweeks = settings.Gameweeks;
            var gw = gameweeks.SingleOrDefault(g => g.Id == message.GameweekId);
            var league = await _leagueClient.GetClassicLeague(message.LeagueId);
            var intro = Formatter.FormatGameweekFinished(gw, league);
            var standings = Formatter.GetStandings(league, gw);
            var topThree = Formatter.GetTopThreeGameweekEntries(league, gw);
            var worst = Formatter.GetWorstGameweekEntry(league, gw);
            await _publisher.PublishToWorkspace(message.WorkspaceId, message.Channel, intro, standings, topThree, worst);
        }
    }

}
