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
using FplBot.Core.Models;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.GameweekLifecycle.Handlers
{
    internal class GameweekEndedHandler : INotificationHandler<GameweekFinished>
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILeagueClient _leagueClient;
        private readonly IGlobalSettingsClient _gameweekClient;
        private readonly ILogger<GameweekEndedHandler> _logger;
        private readonly IMediator _mediator;
        private readonly ISlackTeamRepository _teamRepo;

        public GameweekEndedHandler(ISlackWorkSpacePublisher publisher,
            ISlackTeamRepository teamsRepo,
            ILeagueClient leagueClient,
            IGlobalSettingsClient gameweekClient,
            ILogger<GameweekEndedHandler> logger, IMediator mediator)
        {
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _leagueClient = leagueClient;
            _gameweekClient = gameweekClient;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Handle(GameweekFinished notification, CancellationToken cancellationToken)
        {
            var gameweek = notification.Gameweek.Id;
            var settings = await _gameweekClient.GetGlobalSettings();
            var gameweeks = settings.Gameweeks;
            var gw = gameweeks.SingleOrDefault(g => g.Id == gameweek);
            if (gw == null)
            {
                _logger.LogError("Found no gameweek with id {id}", gameweek);
                return;
            }

            var teams = await _teamRepo.GetAllTeams();
            foreach (var team in teams)
            {
                if (!team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Standings))
                {
                    _logger.LogInformation("Team {team} hasn't subscribed for gw standings, so bypassing it", team.TeamId);
                    continue;
                }

                try
                {
                    await _mediator.Publish(new PublishStandingsCommand(team, gw), cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }
    }

}
