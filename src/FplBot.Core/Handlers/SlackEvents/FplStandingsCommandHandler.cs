using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Data.Abstractions;
using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Core.Handlers
{
    internal class FplStandingsCommandHandler : HandleAppMentionBase
    {
        private readonly IGlobalSettingsClient _globalSettingsClient;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly IMessageSession _session;

        public FplStandingsCommandHandler(IGlobalSettingsClient globalSettingsClient, ISlackTeamRepository teamRepo, IMessageSession session, ILogger<FplStandingsCommandHandler> logger)
        {
            _globalSettingsClient = globalSettingsClient;
            _teamRepo = teamRepo;
            _session = session;
        }

        public override string[] Commands => new[] { "standings" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
        {
            var team = await _teamRepo.GetTeam(eventMetadata.Team_Id);
            var settings =  await _globalSettingsClient.GetGlobalSettings();
            var gameweek = settings.Gameweeks.GetCurrentGameweek();
            if (team.HasChannelAndLeagueSetup())
            {
                await _session.SendLocal(new PublishStandingsToSlackWorkspace(team.TeamId, appMentioned.Channel, team.FplbotLeagueId.Value, gameweek.Id));
            }

            return new EventHandledResponse("OK");
        }
        public override (string,string) GetHelpDescription() => (CommandsFormatted, "Get current league standings");
    }
}
