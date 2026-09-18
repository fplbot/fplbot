using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack;

public class SlackNearDeadlineHandler(
    ISlackTeamRepository teamRepo,
    ISlackWorkSpacePublisher publisher,
    ISlackClientBuilder builder,
    IGlobalSettingsClient globalSettingsClient,
    IFixtureClient fixtures,
    ILogger<SlackNearDeadlineHandler> logger)
    :
        IConsumer<OneHourToDeadline>,
        IConsumer<TwentyFourHoursToDeadline>,
        IConsumer<PublishDeadlineNotificationToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<OneHourToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 60 minutes to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.Deadlines);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            var text = $"<!channel> ⏳ Gameweek {message.GameweekNearingDeadline.Id} deadline in 60 minutes!";
            var command = new PublishToSlack(teamId, channelId, text);
            await context.Publish(command);
        }
    }

    public async Task Consume(ConsumeContext<TwentyFourHoursToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 24h to (gw{message.GameweekNearingDeadline.Id}) deadline");

        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.Deadlines);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            var command = new PublishDeadlineNotificationToSlackWorkspace(teamId, channelId, message.GameweekNearingDeadline);
            await context.Publish(command);
        }
    }

    public async Task Consume(ConsumeContext<PublishDeadlineNotificationToSlackWorkspace> context)
    {
        var message = context.Message;
        var channelId = message.ChannelId;
        var notification = $"⏳ Gameweek {message.Gameweek.Id} deadline in 24 hours!";

        var res = await publisher.PublishToWorkspaceWithResponse(message.TeamId,
            new ChatPostMessageRequest { Channel = channelId, Text = notification });

        if (res is null)
        {
            return;
        }

        var gameweekFixtures = await fixtures.GetFixturesByGameweek(message.Gameweek.Id) ?? [];
        var teams = (await globalSettingsClient.GetGlobalSettings())?.Teams ?? [];
        var fixturesList = Formatter.FixturesForGameweek(message.Gameweek.Id, message.Gameweek.Name,
            message.Gameweek.Deadline, gameweekFixtures, teams, tzOffset: await GetWorkspaceTzOffset());

        await publisher.PublishToWorkspace(message.TeamId,
            new ChatPostMessageRequest { Channel = channelId, thread_ts = res.ts, Text = fixturesList, unfurl_links = "false" });

        async Task<int> GetWorkspaceTzOffset()
        {
            var installation = await teamRepo.GetInstallation(message.TeamId);
            if (installation.Token is null)
            {
                return 0;
            }

            var users = await builder.Build(installation.Token).UsersList();
            return users.Members?.FirstOrDefault(u => u.Is_Admin)?.Tz_Offset ?? 0;
        }
    }
}
