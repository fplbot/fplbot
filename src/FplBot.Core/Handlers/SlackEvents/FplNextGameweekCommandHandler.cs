using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Core.Handlers
{
    public class FplNextGameweekCommandHandler : HandleAppMentionBase
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly ISlackClientBuilder _slackClientService;
        private readonly ITokenStore _tokenStore;
        private readonly IGlobalSettingsClient _gameweekClient;
        private readonly IFixtureClient _fixtureClient;
        private readonly IGlobalSettingsClient _globalSettingsClient;

        public FplNextGameweekCommandHandler(ISlackWorkSpacePublisher workspacePublisher, IGlobalSettingsClient gameweekClient, IFixtureClient fixtureClient, IGlobalSettingsClient globalSettingsClient, ISlackClientBuilder slackClientService, ITokenStore tokenStore)
        {
            _workspacePublisher = workspacePublisher;
            _gameweekClient = gameweekClient;
            _fixtureClient = fixtureClient;
            _globalSettingsClient = globalSettingsClient;
            _slackClientService = slackClientService;
            _tokenStore = tokenStore;
        }

        public override string[] Commands => new[] { "next" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
        {
            var token = await _tokenStore.GetTokenByTeamId(eventMetadata.Team_Id);
            var slackClient = _slackClientService.Build(token);
            var usersTask = slackClient.UsersList();
            var settings = await _globalSettingsClient.GetGlobalSettings();

            var users = await usersTask;
            var gameweeks = settings.Gameweeks;
            var teams = settings.Teams;

            var nextGw = gameweeks.First(gw => gw.IsNext);
            var fixtures = await _fixtureClient.GetFixturesByGameweek(nextGw.Id);

            var user = users.Members.FirstOrDefault(x => x.Id == slackEvent.User);
            var userTzOffset = user?.Tz_Offset ?? 0;

            var textToSend = Formatter.FixturesForGameweek(nextGw, fixtures, teams, userTzOffset);

            await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, slackEvent.Channel, textToSend);
            
            return new EventHandledResponse(textToSend);
        }

        public override (string,string) GetHelpDescription() => (CommandsFormatted, "Displays the fixtures for next gameweek");
    }
}