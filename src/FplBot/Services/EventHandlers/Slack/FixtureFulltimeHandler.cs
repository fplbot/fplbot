using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack;

public class FixtureFulltimeHandler(
    ISlackClientBuilder builder,
    ISlackTeamRepository slackTeamRepo,
    ILogger<FixtureFulltimeHandler> logger,
    IGlobalSettingsClient settingsClient,
    IFixtureClient fixtureClient)
    : IConsumer<FixtureFinished>, IConsumer<PublishFulltimeMessageToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<FixtureFinished> context)
    {
        var message = context.Message;
        logger.LogInformation("Handling fixture full time");
        var teams = await slackTeamRepo.GetAllTeams();
        var settings = await settingsClient.GetGlobalSettings();
        var fixtures = await fixtureClient.GetFixtures() ?? new List<Fpl.Client.Models.Fixture>();
        var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId)!;
        var fixture = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings?.Teams ?? new List<Fpl.Client.Models.Team>(), settings?.Players ?? new List<Fpl.Client.Models.Player>(), fplfixture);
        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
        var threadMessage = Formatter.FormatProvisionalFinished(fixture);

        foreach (var slackTeam in teams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.FixtureFullTime))
            {
                await context.Publish(new PublishFulltimeMessageToSlackWorkspace(slackTeam.TeamId!, title, threadMessage));
            }
        }
    }

    public async Task Consume(ConsumeContext<PublishFulltimeMessageToSlackWorkspace> context)
    {
        var message = context.Message;
        var team = await slackTeamRepo.GetTeam(message.WorkspaceId);
        if (team.AccessToken is not null)
        {
            var slackClient = builder.Build(team.AccessToken);
            var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, message.Title);
            if(!string.IsNullOrEmpty(message.ThreadMessage) && res.Ok)
            {
                await slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = team.FplBotSlackChannel, thread_ts = res.ts, Text = message.ThreadMessage, unfurl_links = "false"
                });
            }
        }
        else
        {
            logger.LogWarning("Slack Workspace '{TeamId}' is missing a token. Not publishing. ", message.WorkspaceId);
        }
    }
}
