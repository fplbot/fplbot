using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers.Slack.Commands;

public class BotJoinedChannelHandler(
    ISlackWorkSpacePublisher publisher,
    ISlackClientBuilder slackClientService,
    ISlackTeamRepository teamRepo,
    ILeagueClient leagueClient,
    IConfiguration configuration)
    : IConsumer<ProcessBotJoinedChannel>
{
    private readonly string? _slackAppId = configuration["SlackAppId"];

    public async Task Consume(ConsumeContext<ProcessBotJoinedChannel> context)
    {
        var command = context.Message;
        var installation = await teamRepo.GetInstallation(command.TeamId);
        var slackClient = slackClientService.Build(installation.Token);
        var userProfile = await slackClient.UserProfile(command.User);
        if (userProfile.Profile.Api_App_Id != _slackAppId)
        {
            return;
        }

        var introMessage = ":wave: Hi, I'm fplbot. Type `@fplbot help` to see what I can do.";
        var setupMessage = await DescribeSetup(installation, command.ChannelId);

        await publisher.PublishToWorkspace(command.TeamId, command.ChannelId, introMessage, setupMessage);
    }

    private async Task<string> DescribeSetup(Installation installation, string joinedChannel)
    {
        var thisChannel = installation.GetChannel(joinedChannel);

        if (thisChannel?.FollowedLeagueId is { } leagueId)
        {
            try
            {
                var league = await leagueClient.GetClassicLeague((int)leagueId.Value);
                return $"I'm already pushing notifications relevant to {league?.Properties?.Name} into this channel.";
            }
            catch (HttpRequestException e) when (e.Message.Contains("404"))
            {
                return
                    $"I'm currently following no valid league here. The invalid leagueid is `{leagueId.Value}`. Use `@fplbot follow` to set up a new valid leagueid.";
            }
        }

        var otherChannels = installation.ChannelSubscriptions.Where(c => c.ChannelId != joinedChannel).ToList();
        if (otherChannels.Count == 0)
        {
            return "To get notifications for a league, use my `@fplbot follow` command in this channel.";
        }

        var channelNames = string.Join(", ", otherChannels.Select(c => ChannelName(c.ChannelId)));
        return
            $"I'm not set up in this channel yet, but I'm already active in {channelNames}. Use `@fplbot follow` in this channel too if you want notifications here as well.";
    }

    // Back-compat as we currently have a mix of:
    // - display names (#name)
    // - channel_ids (C12351)
    // Man be removed next season when we require updates to leagueids
    private static string ChannelName(string channelId) =>
        channelId.StartsWith("#") ? channelId : $"<#{channelId}>";
}
