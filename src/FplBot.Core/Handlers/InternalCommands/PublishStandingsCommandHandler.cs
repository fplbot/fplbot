using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Data.Models;
using MediatR;

namespace FplBot.Core.Handlers.InternalCommands
{
    public record PublishStandingsCommand(SlackTeam Team, Gameweek FinishedGameweek) : INotification;

    internal class PublishStandingsCommandHandler : INotificationHandler<PublishStandingsCommand>
    {
        private readonly ILeagueClient _leagueClient;
        private readonly ISlackWorkSpacePublisher _publisher;

        public PublishStandingsCommandHandler(ILeagueClient leagueClient, ISlackWorkSpacePublisher publisher)
        {
            _leagueClient = leagueClient;
            _publisher = publisher;
        }

        public async Task Handle(PublishStandingsCommand command, CancellationToken cancellationToken)
        {
            var league = await _leagueClient.GetClassicLeague((int) command.Team.FplbotLeagueId);
            var intro = Formatter.FormatGameweekFinished(command.FinishedGameweek, league);
            var standings = Formatter.GetStandings(league, command.FinishedGameweek);
            var topThree = Formatter.GetTopThreeGameweekEntries(league, command.FinishedGameweek);
            var worst = Formatter.GetWorstGameweekEntry(league, command.FinishedGameweek);
            await _publisher.PublishToWorkspace(command.Team.TeamId, command.Team.FplBotSlackChannel, intro, standings, topThree, worst);
        }
    }
}
