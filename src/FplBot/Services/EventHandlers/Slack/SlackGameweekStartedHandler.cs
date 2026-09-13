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
        var installations = await teamsRepo.GetAllInstallations();
        foreach (var installation in installations)
        {
            await context.Publish(new ProcessGameweekStartedForSlackWorkspace(installation.TeamId, notification.NewGameweek.Id));
        }
    }

    public async Task Consume(ConsumeContext<ProcessGameweekStartedForSlackWorkspace> context)
    {
        var message = context.Message;
        var newGameweek = message.GameweekId;

        var installation = await teamsRepo.GetInstallation(message.WorkspaceId);
        var channel = installation.PrimaryChannel();
        var leagueId = channel?.FollowedLeagueId?.Value;

        var messages = new List<string>();

        ClassicLeague? league = null;
        if (leagueId.HasValue)
        {
            league = await leagueClient.GetClassicLeague((int)leagueId.Value, tolerate404:true);
        }

        var leagueExists = league != null;
        var leagueStarted = league?.Properties?.StartEvent is var startEvent && newGameweek >= startEvent;

        if(leagueExists && leagueStarted && (installation.HasRegisteredFor(FplEvent.Captains) || installation.HasRegisteredFor(FplEvent.Transfers)))
            await publisher.PublishToWorkspace(installation.TeamId, channel!.ChannelId, $"Gameweek {message.GameweekId}!");

        if (leagueExists && leagueStarted && installation.HasRegisteredFor(FplEvent.Captains))
        {
            var captainPicks = await captainsByGameweek.GetEntryCaptainPicks(newGameweek, (int)leagueId!.Value);
            if (league!.Standings?.Entries.Count < MemberCountForLargeLeague)
            {
                messages.Add(captainsByGameweek.GetCaptainsByGameWeek(newGameweek, captainPicks));
                messages.Add(captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, captainPicks));
            }
            else
            {
                messages.Add(captainsByGameweek.GetCaptainsStatsByGameWeek(captainPicks));
            }

        }
        else if (leagueId.HasValue && !leagueExists && installation.HasRegisteredFor(FplEvent.Captains))
        {
            messages.Add($"⚠️ You're subscribing to captains notifications, but following a league ({leagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to captains to avoid this message in the future.");
        }
        else
        {
            logger.LogInformation("Bypassing team {team} notifications. League started: {leagueStarted}", installation.TeamId, leagueStarted);
        }

        if (leagueExists && leagueStarted && installation.HasRegisteredFor(FplEvent.Transfers))
        {
            try
            {
                if (league!.Standings?.Entries.Count < MemberCountForLargeLeague)
                {
                    messages.Add(await transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, (int)leagueId!.Value));
                }
                else
                {
                    var externalLink = $"See https://www.fplbot.app/leagues/{leagueId!.Value} for all transfers";
                    messages.Add(externalLink);
                }

            }
            catch(HttpRequestException hre) when(hre.StatusCode == HttpStatusCode.TooManyRequests) // fallback
            {
                var externalLink = $"See https://www.fplbot.app/leagues/{leagueId!.Value} for all transfers";
                messages.Add(externalLink);
            }
        }
        else if (leagueId.HasValue && !leagueExists && installation.HasRegisteredFor(FplEvent.Transfers))
        {
            messages.Add($"⚠️ You're subscribing to transfers notifications, but following a league ({leagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to transfers to avoid this message in the future.");
        }
        else
        {
            logger.LogInformation("Bypassing team {team} notifications. League started: {leagueStarted}", installation.TeamId, leagueStarted);
        }

        await publisher.PublishToWorkspace(installation.TeamId, channel!.ChannelId, messages.ToArray());
    }
}
