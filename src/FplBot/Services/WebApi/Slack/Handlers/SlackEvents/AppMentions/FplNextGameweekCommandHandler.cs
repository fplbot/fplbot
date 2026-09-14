using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Formatting;
using FplBot.Services.WebApi.Slack.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class FplNextGameweekCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    IFixtureClient fixtureClient,
    IGlobalSettingsClient globalSettingsClient,
    ISlackClientBuilder slackClientService,
    ISlackTeamRepository tokenStore)
    : HandleAppMentionBase
{
    public override string[] Commands => ["next"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        var installation = await tokenStore.GetInstallation(eventMetadata.Team_Id);
        var slackClient = slackClientService.Build(installation.Token);
        var usersTask = slackClient.UsersList();
        var settings = await globalSettingsClient.GetGlobalSettings();

        var users = await usersTask;
        var gameweeks = settings?.Gameweeks ?? [];
        var teams = settings?.Teams ?? [];

        var nextGw = gameweeks.First(gw => gw.IsNext);
        var fixtures = await fixtureClient.GetFixturesByGameweek(nextGw.Id) ?? [];

        var user = users.Members.FirstOrDefault(x => x.Id == slackEvent.User);
        var userTzOffset = user?.Tz_Offset ?? 0;

        var textToSend = Formatter.FixturesForGameweek(nextGw.Id, nextGw.Name ?? "", nextGw.Deadline, fixtures, teams, userTzOffset);

        await workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, slackEvent.Channel, textToSend);

        return new EventHandledResponse(textToSend);
    }

    public override (string,string) GetHelpDescription() => (CommandsFormatted, "Displays the fixtures for next gameweek");
}
