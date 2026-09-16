using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack;

public class SlackFixtureFulltimeHandler(
    ISlackClientBuilder builder,
    ISlackTeamRepository slackTeamRepo,
    ILogger<SlackFixtureFulltimeHandler> logger,
    IGlobalSettingsClient settingsClient,
    IFixtureClient fixtureClient,
    ILiveClient liveClient)
    : IConsumer<FixtureFinished>, IConsumer<PublishFulltimeMessageToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<FixtureFinished> context)
    {
        var message = context.Message;
        logger.LogInformation("Handling fixture full time");
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.FixtureFullTime);
        var settings = await settingsClient.GetGlobalSettings();
        var fixtures = await fixtureClient.GetFixtures() ?? [];
        var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId);
        if (fplfixture == null)
        {
            logger.LogWarning("Could not find fixture {FixtureId} in FPL API", message.FixtureId);
            return;
        }
        var liveItems = fplfixture.Event.HasValue
            ? await liveClient.GetLiveItems(fplfixture.Event.Value, isOngoingGameweek: true)
            : null;
        var fixture = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings?.Teams ?? [], settings?.Players ?? [], fplfixture, liveItems);
        var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
        var threadMessage = Formatter.FormatProvisionalFinished(fixture);

        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishFulltimeMessageToSlackWorkspace(teamId, channelId, title, threadMessage));
        }
    }

    public async Task Consume(ConsumeContext<PublishFulltimeMessageToSlackWorkspace> context)
    {
        var message = context.Message;
        var installation = await slackTeamRepo.GetInstallation(message.WorkspaceId);
        if (installation.Token is not null)
        {
            var channelId = message.ChannelId;
            var slackClient = builder.Build(installation.Token);
            var res = await slackClient.ChatPostMessage(channelId, message.Title);
            if(!string.IsNullOrEmpty(message.ThreadMessage) && res.Ok)
            {
                await slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = channelId, thread_ts = res.ts, Text = message.ThreadMessage, unfurl_links = "false"
                });
            }
        }
        else
        {
            logger.LogWarning("Slack Workspace '{TeamId}' is missing a token. Not publishing. ", message.WorkspaceId);
        }
    }
}
