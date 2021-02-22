using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
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
        private readonly IGlobalSettingsClient _globalSettingsClient;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly IMediator _mediator;

        public FplStandingsCommandHandler(ISlackWorkSpacePublisher workspacePublisher, IGlobalSettingsClient globalSettingsClient, ILeagueClient leagueClient, ISlackTeamRepository teamRepo, IMediator mediator, ILogger<FplStandingsCommandHandler> logger)
        {
            _globalSettingsClient = globalSettingsClient;
            _teamRepo = teamRepo;
            _mediator = mediator;
        }

        public override string[] Commands => new[] { "standings" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
        {
            var team = await _teamRepo.GetTeam(eventMetadata.Team_Id);
            var settings =  await _globalSettingsClient.GetGlobalSettings();
            var gameweek = settings.Gameweeks.GetCurrentGameweek();
            await _mediator.Publish(new PublishStandingsCommand(team, gameweek));
            return new EventHandledResponse("OK");
        }
        public override (string,string) GetHelpDescription() => (CommandsFormatted, "Get current league standings");
    }
}