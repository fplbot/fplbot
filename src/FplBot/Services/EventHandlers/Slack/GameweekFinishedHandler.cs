using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

internal class GameweekFinishedHandler : IConsumer<GameweekFinished>, IConsumer<PublishStandingsToSlackWorkspace>
{
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly ILeagueClient _leagueClient;
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly ISlackTeamRepository _teamRepo;

    public GameweekFinishedHandler(ISlackWorkSpacePublisher publisher,
        ISlackTeamRepository teamsRepo,
        ILeagueClient leagueClient,
        IGlobalSettingsClient settingsClient)
    {
        _publisher = publisher;
        _teamRepo = teamsRepo;
        _leagueClient = leagueClient;
        _settingsClient = settingsClient;
    }

    public async Task Consume(ConsumeContext<GameweekFinished> context)
    {
        var notification = context.Message;
        var teams = await _teamRepo.GetAllTeams();
        foreach (var team in teams)
        {
            if (team.HasRegisteredFor(EventSubscription.Standings))
            {
                await context.Publish(new PublishStandingsToSlackWorkspace(team.TeamId!, team.FplBotSlackChannel!, team.FplbotLeagueId!.Value, notification.FinishedGameweek.Id));
            }
        }
    }

    public async Task Consume(ConsumeContext<PublishStandingsToSlackWorkspace> context)
    {
        var message = context.Message;
        var settings = await _settingsClient.GetGlobalSettings();
        var gameweeks = settings?.Gameweeks ?? new List<Gameweek>();
        var gw = gameweeks.SingleOrDefault(g => g.Id == message.GameweekId);
        try
        {
            var league = await _leagueClient.GetClassicLeague(message.LeagueId);
            if (league == null) return;
            var leagueStarted = league.Properties?.StartEvent is var startEvent && message.GameweekId >= startEvent;
            if (leagueStarted && gw != null)
            {
                var intro = Formatter.FormatGameweekFinished(gw, league);
                var standings = Formatter.GetStandings(league, gw);
                var topThree = Formatter.GetTopThreeGameweekEntries(league, gw);
                var worst = Formatter.GetWorstGameweekEntry(league, gw);

                var messages = new List<string> { intro, standings, topThree ?? string.Empty};
                if (worst is not null)
                {
                    messages.Add(worst);
                }
                await _publisher.PublishToWorkspace(message.WorkspaceId, message.Channel, messages.ToArray());
            }
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            await _publisher.PublishToWorkspace(message.WorkspaceId, message.Channel, $"League standings are now generally ready, but I could not seem to find a classic league with id `{message.LeagueId}`. Are you sure it's a valid classic league id?");
        }
    }
}
