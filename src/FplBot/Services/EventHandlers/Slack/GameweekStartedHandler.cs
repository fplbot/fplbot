using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

internal class GameweekStartedHandler(
    ICaptainsByGameWeek captainsByGameweek,
    ITransfersByGameWeek transfersByGameweek,
    ISlackWorkSpacePublisher publisher,
    ISlackTeamRepository teamsRepo,
    ILeagueClient leagueClient,
    ILogger<GameweekStartedHandler> logger)
    : IConsumer<GameweekJustBegan>, IConsumer<ProcessGameweekStartedForSlackWorkspace>
{
    private const int MemberCountForLargeLeague = 25;

    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        var notification = context.Message;
        var teams = await teamsRepo.GetAllTeams();
        foreach (var team in teams)
        {
            await context.Publish(new ProcessGameweekStartedForSlackWorkspace(team.TeamId!, notification.NewGameweek.Id));
        }
    }

    public async Task Consume(ConsumeContext<ProcessGameweekStartedForSlackWorkspace> context)
    {
        var message = context.Message;
        var newGameweek = message.GameweekId;

        var team = await teamsRepo.GetTeam(message.WorkspaceId);

        var messages = new List<string>();

        ClassicLeague? league = null;
        if (team.FplbotLeagueId.HasValue)
        {
            league = await leagueClient.GetClassicLeague(team.FplbotLeagueId.Value, tolerate404:true);
        }

        var leagueExists = league != null;
        var leagueStarted = league?.Properties?.StartEvent is var startEvent && newGameweek >= startEvent;

        if(leagueExists && leagueStarted && (team.HasRegisteredFor(EventSubscription.Captains) || team.HasRegisteredFor(EventSubscription.Transfers)))
            await publisher.PublishToWorkspace(team.TeamId!, team.FplBotSlackChannel!, $"Gameweek {message.GameweekId}!");

        if (leagueExists && leagueStarted && team.HasRegisteredFor(EventSubscription.Captains))
        {
            var captainPicks = await captainsByGameweek.GetEntryCaptainPicks(newGameweek, team.FplbotLeagueId!.Value);
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
        else if (team.FplbotLeagueId.HasValue && !leagueExists && team.HasRegisteredFor(EventSubscription.Captains))
        {
            messages.Add($"⚠️ You're subscribing to captains notifications, but following a league ({team.FplbotLeagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to captains to avoid this message in the future.");
        }
        else
        {
            logger.LogInformation("Bypassing team {team} notifications. League started: {leagueStarted}", team.TeamId, leagueStarted);
        }

        if (leagueExists && leagueStarted && team.HasRegisteredFor(EventSubscription.Transfers))
        {
            try
            {
                if (league!.Standings?.Entries.Count < MemberCountForLargeLeague)
                {
                    messages.Add(await transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, team.FplbotLeagueId!.Value));
                }
                else
                {
                    var externalLink = $"See https://www.fplbot.app/leagues/{team.FplbotLeagueId!.Value} for all transfers";
                    messages.Add(externalLink);
                }

            }
            catch(HttpRequestException hre) when(hre.StatusCode == HttpStatusCode.TooManyRequests) // fallback
            {
                var externalLink = $"See https://www.fplbot.app/leagues/{team.FplbotLeagueId!.Value} for all transfers";
                messages.Add(externalLink);
            }
        }
        else if (team.FplbotLeagueId.HasValue && !leagueExists && team.HasRegisteredFor(EventSubscription.Transfers))
        {
            messages.Add($"⚠️ You're subscribing to transfers notifications, but following a league ({team.FplbotLeagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to transfers to avoid this message in the future.");
        }
        else
        {
            logger.LogInformation("Bypassing team {team} notifications. League started: {leagueStarted}", team.TeamId, leagueStarted);
        }

        await publisher.PublishToWorkspace(team.TeamId!, team.FplBotSlackChannel!, messages.ToArray());
    }
}
