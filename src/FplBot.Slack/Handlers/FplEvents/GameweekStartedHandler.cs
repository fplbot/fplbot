using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using Microsoft.Extensions.Logging;
using Nest;
using NServiceBus;

namespace FplBot.Slack.Handlers.FplEvents;

internal class GameweekStartedHandler : IHandleMessages<GameweekJustBegan>, IHandleMessages<ProcessGameweekStartedForSlackWorkspace>
{
    private const int MemberCountForLargeLeague = 25;
    private readonly ICaptainsByGameWeek _captainsByGameweek;
    private readonly ITransfersByGameWeek _transfersByGameweek;
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly ISlackTeamRepository _teamRepo;
    private readonly ILogger<GameweekStartedHandler> _logger;
    private readonly ILeagueClient _leagueClient;

    public GameweekStartedHandler(ICaptainsByGameWeek captainsByGameweek,
        ITransfersByGameWeek transfersByGameweek,
        ISlackWorkSpacePublisher publisher,
        ISlackTeamRepository teamsRepo,
        ILeagueClient leagueClient,
        ILogger<GameweekStartedHandler> logger)
    {
        _captainsByGameweek = captainsByGameweek;
        _transfersByGameweek = transfersByGameweek;
        _publisher = publisher;
        _teamRepo = teamsRepo;
        _logger = logger;
        _leagueClient = leagueClient;
    }

    public async Task Handle(GameweekJustBegan notification, IMessageHandlerContext context)
    {
        var teams = await _teamRepo.GetAllTeams();
        foreach (var team in teams)
        {
            var options = new SendOptions();
            options.RequireImmediateDispatch();
            options.RouteToThisEndpoint();
            await context.Send(new ProcessGameweekStartedForSlackWorkspace(team.TeamId, notification.NewGameweek.Id), options);
        }
    }

    public async Task Handle(ProcessGameweekStartedForSlackWorkspace message, IMessageHandlerContext context)
    {
        var newGameweek = message.GameweekId;

        var team = await _teamRepo.GetTeam(message.WorkspaceId);
        if(team.HasRegisteredFor(EventSubscription.Captains) || team.HasRegisteredFor(EventSubscription.Transfers))
            await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, $"Gameweek {message.GameweekId}!");

        var messages = new List<string>();

        var leagueExists = false;
        ClassicLeague league = null;
        if (team.FplbotLeagueId.HasValue)
        {
            league = await _leagueClient.GetClassicLeague(team.FplbotLeagueId.Value, tolerate404:true);
        }
        leagueExists = league != null;

        if (leagueExists && team.HasRegisteredFor(EventSubscription.Captains))
        {
            var captainPicks = await _captainsByGameweek.GetEntryCaptainPicks(newGameweek, team.FplbotLeagueId.Value);
            if (league.Standings.Entries.Count < MemberCountForLargeLeague)
            {
                messages.Add(await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek, captainPicks));
                messages.Add(await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, captainPicks));
            }
            else
            {
                messages.Add(await _captainsByGameweek.GetCaptainsStatsByGameWeek(captainPicks));
            }

        }
        else if (team.FplbotLeagueId.HasValue && !leagueExists && team.HasRegisteredFor(EventSubscription.Captains))
        {
            messages.Add($"⚠️ You're subscribing to captains notifications, but following a league ({team.FplbotLeagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to captains to avoid this message in the future.");
        }
        else
        {
            _logger.LogInformation("Team {team} hasn't subscribed for gw start captains, so bypassing it", team.TeamId);
        }

        if (leagueExists && team.HasRegisteredFor(EventSubscription.Transfers))
        {
            try
            {
                if (league.Standings.Entries.Count < MemberCountForLargeLeague)
                {
                    messages.Add(await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, team.FplbotLeagueId.Value));
                }
                else
                {
                    var externalLink = $"See https://www.fplbot.app/leagues/{team.FplbotLeagueId.Value} for all transfers";
                    messages.Add(externalLink);
                }

            }
            catch(HttpRequestException hre) when(hre.StatusCode == HttpStatusCode.TooManyRequests) // fallback
            {
                var externalLink = $"See https://www.fplbot.app/leagues/{team.FplbotLeagueId.Value} for all transfers";
                messages.Add(externalLink);
            }
        }
        else if (team.FplbotLeagueId.HasValue && !leagueExists && team.HasRegisteredFor(EventSubscription.Transfers))
        {
            messages.Add($"⚠️ You're subscribing to transfers notifications, but following a league ({team.FplbotLeagueId.Value}) that does not exist. Update to a valid classic league, or unsubscribe to transfers to avoid this message in the future.");
        }
        else
        {
            _logger.LogInformation("Team {team} hasn't subscribed for gw start transfers, so bypassing it", team.TeamId);
        }

        await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, messages.ToArray());
    }
}
