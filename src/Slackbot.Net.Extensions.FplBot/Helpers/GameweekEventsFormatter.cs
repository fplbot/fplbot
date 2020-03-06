using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Models;
using System.Collections.Generic;
using System.Linq;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class GameweekEventsFormatter
    {
        private readonly string[] _transferredGoalScorerOutTaunts =
        {
            "Ah jiiz, you transferred him out, {0} :joy:",
            "You just had to knee jerk him out, didn't you, {0}?",
            "Didn't you have that guy last week, {0}?",
            "Goddammit, really? You couldn't hold on to him just one more gameweek, {0}?"
        };

        public GameweekEventsFormatter(ILogger<GameweekEventsFormatter> logger)
        {
        }

        public List<string> FormatNewFixtureEvents(
            List<FixtureEvents> newFixtureEvents,
            IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek,
            ICollection<Player> players,
            ICollection<Team> teams)
        {
            var formattedStrings = new List<string>();

            newFixtureEvents.ForEach(newFixtureEvent =>
            {
                var scoreHeading = $"{GetScore(teams, newFixtureEvent)}\n";
                var eventMessages = newFixtureEvent.StatMap.Select(stat =>
                {
                    switch (stat.Key)
                    {
                        case StatType.GoalsScored:
                            return FormatNewGoals(stat.Value, players, transfersForCurrentGameweek);
                        case StatType.Assists:
                            return Enumerable.Empty<string>();
                        case StatType.OwnGoals:
                            return Enumerable.Empty<string>();
                        case StatType.RedCards:
                            return Enumerable.Empty<string>();
                        case StatType.PenaltiesMissed:
                            return Enumerable.Empty<string>();
                        case StatType.PenaltiesSaved:
                            return Enumerable.Empty<string>();
                        default: return Enumerable.Empty<string>();
                    }
                });
                formattedStrings.Add(scoreHeading + eventMessages.Select(s => $":black_small_square: {s}\n"));
            });

            return formattedStrings;
        }

        private static string GetScore(ICollection<Team> teams, FixtureEvents fixtureEvent)
        {
            var gameScore = fixtureEvent.GameScore;
            return $"{teams.Single(team => team.Id == gameScore.HomeTeamId).Name} " +
                   $"{gameScore.HomeTeamScore} - {gameScore.AwayTeamScore} " +
                   $"{teams.Single(team => team.Id == gameScore.AwayTeamId).Name}";
        }

        private List<string> FormatNewGoals(
            List<PlayerEvent> newGoalEvents, 
            ICollection<Player> players, 
            IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek)
        {
            return newGoalEvents.Select(g =>
            {
                var player = players.Single(x => x.Id == g.PlayerId);
                var message = $":soccer: {player.FirstName} {player.SecondName} just scored a goal!";

                if (g.IsRemoved)
                {
                    message = $"~{message}~ (VAR? :shrug:)";
                }
                else
                {
                    var entriesTransferredPlayerOut = EntriesThatTransferredPlayerOutThisGameweek(player.Id, transfersForCurrentGameweek).ToArray();
                    if (entriesTransferredPlayerOut.Any())
                    {
                        message += $" {string.Format(_transferredGoalScorerOutTaunts.GetRandom(), string.Join(", ", entriesTransferredPlayerOut))}";
                    }
                }

                return message;
            }).WhereNotNull().ToList();
        }

        private IEnumerable<string> EntriesThatTransferredPlayerOutThisGameweek(int playerId, IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek)
        {
            return transfersForCurrentGameweek == null ? 
                Enumerable.Empty<string>() : 
                transfersForCurrentGameweek.Where(x => x.PlayerTransferredOut == playerId).Select(x => x.EntryName);
        }
    }
}