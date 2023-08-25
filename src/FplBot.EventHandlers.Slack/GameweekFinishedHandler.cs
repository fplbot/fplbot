using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Slack;

internal class GameweekFinishedHandler : IHandleMessages<GameweekFinished>, IHandleMessages<PublishStandingsToSlackWorkspace>
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

    public async Task Handle(GameweekFinished notification, IMessageHandlerContext context)
    {
        var teams = await _teamRepo.GetAllTeams();
        foreach (var team in teams)
        {
            if (team.HasRegisteredFor(EventSubscription.Standings))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishStandingsToSlackWorkspace(team.TeamId, team.FplBotSlackChannel, team.FplbotLeagueId.Value, notification.FinishedGameweek.Id), options);
            }
        }
    }

    public async Task Handle(PublishStandingsToSlackWorkspace message, IMessageHandlerContext context)
    {
        var settings = await _settingsClient.GetGlobalSettings();
        var gameweeks = settings.Gameweeks;
        var gw = gameweeks.SingleOrDefault(g => g.Id == message.GameweekId);
        ClassicLeague league = null;
        try
        {
            league = await _leagueClient.GetClassicLeague(message.LeagueId);
            var intro = Formatter.FormatGameweekFinished(gw, league);
            var standings = Formatter.GetStandings(league, gw);
            var topThree = Formatter.GetTopThreeGameweekEntries(league, gw);
            var worst = Formatter.GetWorstGameweekEntry(league, gw);

            var messages = new List<string> { intro, standings, topThree};
            if (worst is not null)
            {
                messages.Add(worst);
            }
            await _publisher.PublishToWorkspace(message.WorkspaceId, message.Channel, messages.ToArray());
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            await _publisher.PublishToWorkspace(message.WorkspaceId, message.Channel, $"League standings are now generally ready, but I could not seem to find a classic league with id `{message.LeagueId}`. Are you sure it's a valid classic league id?");
        }
    }
}
