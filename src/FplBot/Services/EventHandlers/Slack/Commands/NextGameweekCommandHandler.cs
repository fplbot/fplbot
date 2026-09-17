using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers.Slack.Commands;

public class NextGameweekCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    IFixtureClient fixtureClient,
    IGlobalSettingsClient globalSettingsClient,
    ISlackClientBuilder slackClientService,
    ISlackTeamRepository tokenStore)
    : IConsumer<ProcessNextGameweekCommand>
{
    public async Task Consume(ConsumeContext<ProcessNextGameweekCommand> context)
    {
        var command = context.Message;
        var installation = await tokenStore.GetInstallation(command.TeamId);
        var slackClient = slackClientService.Build(installation.Token);
        var usersTask = slackClient.UsersList();
        var settings = await globalSettingsClient.GetGlobalSettings();

        var users = await usersTask;
        var gameweeks = settings?.Gameweeks ?? [];
        var teams = settings?.Teams ?? [];

        var nextGw = gameweeks.First(gw => gw.IsNext);
        var fixtures = await fixtureClient.GetFixturesByGameweek(nextGw.Id) ?? [];

        var user = users.Members.FirstOrDefault(x => x.Id == command.User);
        var userTzOffset = user?.Tz_Offset ?? 0;

        var textToSend = Formatter.FixturesForGameweek(nextGw.Id, nextGw.Name ?? "", nextGw.Deadline, fixtures, teams, userTzOffset);

        await workspacePublisher.PublishToWorkspace(command.TeamId, command.Channel, textToSend);
    }
}
