using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

internal class SlackGameweekStartedHandler(
    ICaptainsByGameWeek captainsByGameweek,
    ITransfersByGameWeek transfersByGameweek,
    ISlackWorkSpacePublisher publisher,
    ISlackTeamRepository teamsRepo,
    ILeagueClient leagueClient,
    ILogger<SlackGameweekStartedHandler> logger)
    : IConsumer<GameweekJustBegan>, IConsumer<ProcessGameweekStartedForSlackWorkspace>
{
    private const int MemberCountForLargeLeague = 25;

    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        var notification = context.Message;
        var subscribedChannels = await teamsRepo.GetChannelsSubscribedTo(FplEvent.Captains, FplEvent.Transfers);
        var subscribedInstallationIds = subscribedChannels.Select(c => c.InstallationId).Distinct();
        foreach (var installationId in subscribedInstallationIds)
        {
            await context.Publish(new ProcessGameweekStartedForSlackWorkspace(installationId, notification.NewGameweek.Id));
        }
    }

    public async Task Consume(ConsumeContext<ProcessGameweekStartedForSlackWorkspace> context)
    {
        var message = context.Message;
        var newGameweek = message.GameweekId;

        var installation = await teamsRepo.GetInstallation(message.WorkspaceId);
        foreach (var sub in installation.ChannelSubscriptions)
        {
            await DoSubHandling(installation.Id, sub, newGameweek);
        }
    }

    private async Task DoSubHandling(string teamId, ChannelSubscription sub, int newGameweek)
    {
        var messages = new List<string>();
        ClassicLeague? league = null;
        if (sub.FollowedLeagueId is not null)
        {
            league = await leagueClient.GetClassicLeague((int)sub.FollowedLeagueId.Value, tolerate404:true);
        }

        var leagueExists = league != null;
        var leagueStarted = league?.Properties?.StartEvent is var startEvent && newGameweek >= startEvent;

        if(leagueExists && leagueStarted)
        {
            if (sub.IsSubscribedTo(FplEvent.Captains) || sub.IsSubscribedTo(FplEvent.Transfers))
            {
                await publisher.PublishToWorkspace(teamId, sub.ChannelId, $"Gameweek {newGameweek}!");
            }
        }

        if (league is {Standings: {} standings} && leagueStarted && sub.IsSubscribedTo(FplEvent.Captains))
        {
            var captainPicks = await captainsByGameweek.GetEntryCaptainPicks(newGameweek, (int)sub.FollowedLeagueId!.Value);
            if (standings.Entries.Count < MemberCountForLargeLeague)
            {
                messages.Add(captainsByGameweek.GetCaptainsByGameWeek(newGameweek, captainPicks));
                messages.Add(captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, captainPicks));
            }
            else
            {
                messages.Add(captainsByGameweek.GetCaptainsStatsByGameWeek(captainPicks));
            }

        }
        else if (sub.FollowedLeagueId is {} && !leagueExists && sub.IsSubscribedTo(FplEvent.Captains))
        {
            messages.Add($"⚠️ You're subscribing to captains notifications, but following a league ({sub.FollowedLeagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to captains to avoid this message in the future.");
        }
        else
        {
            logger.LogInformation("Bypassing team {team} notifications. League started: {leagueStarted}", teamId, leagueStarted);
        }

        if (leagueExists && leagueStarted && sub.IsSubscribedTo(FplEvent.Transfers))
        {
            try
            {
                if (league!.Standings?.Entries.Count < MemberCountForLargeLeague)
                {
                    messages.Add(await transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, (int)sub.FollowedLeagueId!.Value));
                }
                else
                {
                    var externalLink = $"See https://www.fplbot.app/leagues/{sub.FollowedLeagueId!.Value} for all transfers";
                    messages.Add(externalLink);
                }

            }
            catch(HttpRequestException hre) when(hre.StatusCode == HttpStatusCode.TooManyRequests) // fallback
            {
                var externalLink = $"See https://www.fplbot.app/leagues/{sub.FollowedLeagueId!.Value} for all transfers";
                messages.Add(externalLink);
            }
        }
        else if (sub.FollowedLeagueId is {} && !leagueExists && sub.IsSubscribedTo(FplEvent.Transfers))
        {
            messages.Add($"⚠️ You're subscribing to transfers notifications, but following a league ({sub.FollowedLeagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to transfers to avoid this message in the future.");
        }
        else
        {
            logger.LogInformation("Bypassing team {team} notifications. League started: {leagueStarted}", teamId, leagueStarted);
        }

        await publisher.PublishToWorkspace(teamId, sub.ChannelId, messages.ToArray());
    }
}
