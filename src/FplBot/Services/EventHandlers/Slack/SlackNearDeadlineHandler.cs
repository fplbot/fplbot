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
        string notification = $"⏳ Gameweek {message.Gameweek.Id} deadline in 24 hours!";

        var installation = await teamRepo.GetInstallation(message.WorkspaceId);
        var channelId = message.ChannelId;
        if (installation.Token is not null)
        {
            await PublishToTeam();
        }
        else
        {
            logger.LogWarning("Slack Workspace '{TeamId}' is missing a token. Not publishing. ", message.WorkspaceId);
        }


        async Task PublishToTeam()
        {
            var slackClient = builder.Build(installation.Token);
            var res = await SlackDelivery.Post(context, teamRepo, message.WorkspaceId, channelId, logger,
                () => slackClient.ChatPostMessage(channelId, notification));
            if (res is not null)
            {
                await PublishFixtures(slackClient, res.ts);
            }
        }

        async Task PublishFixtures(ISlackClient slackClient, string ts)
        {
            var fixtures1 = await fixtures.GetFixturesByGameweek(message.Gameweek.Id) ?? [];
            var teams = (await globalSettingsClient.GetGlobalSettings())?.Teams ?? [];
            var users = await slackClient.UsersList();
            var user = users.Members?.FirstOrDefault(u =>
                u.Is_Admin); // could have selected app_install user here, if we had this stored
            var userTzOffset = user?.Tz_Offset ?? 0;
            var messageGameweekNearingDeadline = message.Gameweek;
            var fixturesList = Formatter.FixturesForGameweek(messageGameweekNearingDeadline.Id,
                messageGameweekNearingDeadline.Name, messageGameweekNearingDeadline.Deadline, fixtures1, teams,
                tzOffset: userTzOffset);

            await SlackDelivery.Post(context, teamRepo, message.WorkspaceId, channelId, logger,
                () => slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = channelId,
                    thread_ts = ts,
                    Text = fixturesList,
                    unfurl_links = "false"
                }));
        }
    }
}
