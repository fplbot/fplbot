using System;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Core.Handlers
{
    internal class FplStandingsCommandHandler : HandleAppMentionBase
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILeagueClient _leagueClient;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly IMediator _mediator;
        private readonly ILogger<FplStandingsCommandHandler> _logger;

        public FplStandingsCommandHandler(ISlackWorkSpacePublisher workspacePublisher, IGameweekClient gameweekClient, ILeagueClient leagueClient, ISlackTeamRepository teamRepo, IMediator mediator, ILogger<FplStandingsCommandHandler> logger)
        {
            _workspacePublisher = workspacePublisher;
            _gameweekClient = gameweekClient;
            _leagueClient = leagueClient;
            _teamRepo = teamRepo;
            _mediator = mediator;
            _logger = logger;
        }

        public override string[] Commands => new[] { "standings" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
        {
            var team = await _teamRepo.GetTeam(eventMetadata.Team_Id);
            var gameweeks = await _gameweekClient.GetGameweeks();
            var gameweek = gameweeks.GetCurrentGameweek();
            await _mediator.Publish(new PublishStandingsCommand(team, gameweek));
            return new EventHandledResponse("OK");
        }
        public override (string,string) GetHelpDescription() => (CommandsFormatted, "Get current league standings");
    }
}