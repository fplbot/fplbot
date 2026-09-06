using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Formatting;
using FplBot.WebApi.Slack.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class FplNextGameweekCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    IFixtureClient fixtureClient,
    IGlobalSettingsClient globalSettingsClient,
    ISlackClientBuilder slackClientService,
    ISlackTeamRepository tokenStore)
    : HandleAppMentionBase
{
    public override string[] Commands => new[] { "next" };

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        var team = await tokenStore.GetTeam(eventMetadata.Team_Id);
        var slackClient = slackClientService.Build(team.AccessToken);
        var usersTask = slackClient.UsersList();
        var settings = await globalSettingsClient.GetGlobalSettings();

        var users = await usersTask;
        var gameweeks = settings?.Gameweeks ?? new List<Fpl.Client.Models.Gameweek>();
        var teams = settings?.Teams ?? new List<Fpl.Client.Models.Team>();

        var nextGw = gameweeks.First(gw => gw.IsNext);
        var fixtures = await fixtureClient.GetFixturesByGameweek(nextGw.Id) ?? new List<Fpl.Client.Models.Fixture>();

        var user = users.Members.FirstOrDefault(x => x.Id == slackEvent.User);
        var userTzOffset = user?.Tz_Offset ?? 0;

        var textToSend = Formatter.FixturesForGameweek(nextGw.Id, nextGw.Name ?? "", nextGw.Deadline, fixtures, teams, userTzOffset);

        await workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, slackEvent.Channel, textToSend);

        return new EventHandledResponse(textToSend);
    }

    public override (string,string) GetHelpDescription() => (CommandsFormatted, "Displays the fixtures for next gameweek");
}
