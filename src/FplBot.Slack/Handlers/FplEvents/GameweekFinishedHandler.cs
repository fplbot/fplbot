using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using NServiceBus;

namespace FplBot.Slack.Handlers.FplEvents;

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
                await context.SendLocal(new PublishStandingsToSlackWorkspace(team.TeamId, team.FplBotSlackChannel, team.FplbotLeagueId.Value, notification.FinishedGameweek.Id));
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
            await _publisher.PublishToWorkspace(message.WorkspaceId, message.Channel, intro, standings, topThree, worst);
        }
        catch (HttpRequestException e) when (e.Message.Contains("404"))
        {
            await _publisher.PublishToWorkspace(message.WorkspaceId, message.Channel, $"League standings are now generally ready, but I could not seem to find a classic league with id `{message.LeagueId}`. Are you sure it's a valid classic league id?");
        }
    }
}