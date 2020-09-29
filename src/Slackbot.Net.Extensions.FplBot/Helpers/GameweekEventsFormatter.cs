using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class GameweekEventsFormatter
    {

        public static List<string> FormatNewFixtureEvents(List<FixtureEvents> newFixtureEvents, GameweekLeagueContext context)
        {
            var formattedStrings = new List<string>();

            newFixtureEvents.ForEach(newFixtureEvent =>
            {
                var scoreHeading = $"{GetScore(context.Teams, newFixtureEvent)}\n";
                var eventMessages = newFixtureEvent.StatMap.SelectMany(stat =>
                {
                    switch (stat.Key)
                    {
                        case StatType.GoalsScored:
                            return FormatNewGoals(stat.Value, context.Players, context.TransfersForLeague, context.Users);
                        case StatType.Assists:
                            return FormatNewAssists(stat.Value, context.Players);
                        case StatType.OwnGoals:
                            return FormatOwnGoals(stat.Value, context.Players, context.GameweekEntries, context.Users);
                        case StatType.RedCards:
                            return FormatNewRedCards(stat.Value, context.Players, context.TransfersForLeague, context.Users);
                        case StatType.PenaltiesMissed:
                            return FormatNewPenaltiesMissed(stat.Value, context.Players, context.GameweekEntries, context.Users);
                        case StatType.PenaltiesSaved:
                            return FormatNewPenaltiesSaved(stat.Value, context.Players);
                        default: return Enumerable.Empty<string>();
                    }
                }).MaterializeToArray();
                if (eventMessages.Any())
                {
                    formattedStrings.Add(scoreHeading + string.Join("\n", eventMessages.Select(s => $":black_small_square: {s}")));
                }
            });

            return formattedStrings;
        }

        private static IEnumerable<object> FormatNewAssists(
            List<PlayerEvent> newAssistEvents, 
            ICollection<Player> players)
        {
            return FormatEvent(
                newAssistEvents,
                players,
                "got an assist!",
                ":handshake:");
        }

        private static IEnumerable<object> FormatOwnGoals(
            List<PlayerEvent> newOwnGoalEvents,
            ICollection<Player> players,
            IEnumerable<GameweekEntry> gameweekEntries,
            IEnumerable<User> users)
        {
            return FormatEvent(
                newOwnGoalEvents,
                players,
                "scored a goal! In his own goal!",
                ":face_palm:",
                player =>
                {
                    var entriesThatOwnPlayer = EntriesThatHasPlayerInTeam(player.Id, gameweekEntries, users).ToArray();
                    return entriesThatOwnPlayer.Any() ? $" {string.Format(Constants.EventMessages.OwningPlayerWithOwnGoalTaunts.GetRandom(), string.Join(", ", entriesThatOwnPlayer))}" : null;
                });
        }

        private static IEnumerable<object> FormatNewPenaltiesMissed(
            List<PlayerEvent> newPenaltiesMissedEvents,
            ICollection<Player> players,
            IEnumerable<GameweekEntry> gameweekEntries,
            IEnumerable<User> users)
        {
            return FormatEvent(
                newPenaltiesMissedEvents,
                players,
                "missed a penalty!",
                ":dizzy_face:",
                player =>
                {
                    var entriesThatOwnPlayer = EntriesThatHasPlayerInTeam(player.Id, gameweekEntries, users).ToArray();
                    return entriesThatOwnPlayer.Any() ? $" {string.Format(Constants.EventMessages.MissedPenaltyTaunts.GetRandom(), string.Join(", ", entriesThatOwnPlayer))}" : null;
                });
        }

        private static IEnumerable<object> FormatNewPenaltiesSaved(
            List<PlayerEvent> newPenaltiesSavedEvents,
            ICollection<Player> players)
        {
            return FormatEvent(
                newPenaltiesSavedEvents,
                players,
                "saved a penalty!",
                ":man-cartwheeling:");
        }

        private static List<string> FormatNewGoals(
            List<PlayerEvent> newGoalEvents, 
            ICollection<Player> players, 
            IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek,
            IEnumerable<User> users)
        {
            return FormatEvent(
                newGoalEvents,
                players,
                "scored a goal!",
                ":soccer:",
                player =>
                {
                    var entriesTransferredPlayerOut = EntriesThatTransferredPlayerOutThisGameweek(player.Id, transfersForCurrentGameweek, users).ToArray();
                    return entriesTransferredPlayerOut.Any() ? $" {string.Format(Constants.EventMessages.TransferredGoalScorerOutTaunts.GetRandom(), string.Join(", ", entriesTransferredPlayerOut))}" : null;
                });
        }

        private static List<string> FormatNewRedCards(
            List<PlayerEvent> newRedCardEvents,
            ICollection<Player> players,
            IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek,
            IEnumerable<User> users)
        {
            return FormatEvent(
                newRedCardEvents,
                players,
                "got a red card!",
                ":red_circle:",
                player =>
                {
                    var entriesTransferredPlayerIn = EntriesThatTransferredPlayerInThisGameweek(player.Id, transfersForCurrentGameweek, users).ToArray();
                    return entriesTransferredPlayerIn.Any() ? $" {string.Format(Constants.EventMessages.TransferredInRedCardPlayerTaunts.GetRandom(), string.Join(", ", entriesTransferredPlayerIn))}" : null;
                });
        }

        private static List<string> FormatEvent(
            List<PlayerEvent> newGoalEvents,
            ICollection<Player> players,
            string eventDescription,
            string eventEmoji,
            Func<Player, string> append = null)
        {
            return newGoalEvents.Select(g =>
            {
                var player = players.Single(x => x.Id == g.PlayerId);
                var message = $"{player.FirstName} {player.SecondName} {eventDescription} {eventEmoji} ";

                if (g.IsRemoved)
                {
                    message = $"~{message.TrimEnd()}~ (VAR? :shrug:)";
                }
                else
                {
                    message += append?.Invoke(player);
                }

                return message;

            }).WhereNotNull().ToList();
        }

        private static string GetScore(ICollection<Team> teams, FixtureEvents fixtureEvent)
        {
            var gameScore = fixtureEvent.GameScore;
            return $"{teams.Single(team => team.Id == gameScore.HomeTeamId).Name} " +
                   $"{gameScore.HomeTeamScore}-{gameScore.AwayTeamScore} " +
                   $"{teams.Single(team => team.Id == gameScore.AwayTeamId).Name}";
        }

        private static IEnumerable<string> EntriesThatTransferredPlayerOutThisGameweek(int playerId, IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek, IEnumerable<User> users)
        {
            return transfersForCurrentGameweek == null ? 
                Enumerable.Empty<string>() : 
                transfersForCurrentGameweek.Where(x => x.PlayerTransferredOut == playerId).Select(x => SlackHandleHelper.GetSlackHandleOrFallback(users, x.EntryName));
        }

        private static IEnumerable<string> EntriesThatTransferredPlayerInThisGameweek(int playerId, IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek, IEnumerable<User> users)
        {
            return transfersForCurrentGameweek == null ?
                Enumerable.Empty<string>() :
                transfersForCurrentGameweek.Where(x => x.PlayerTransferredIn == playerId).Select(x => SlackHandleHelper.GetSlackHandleOrFallback(users, x.EntryName));
        }

        private static IEnumerable<string> EntriesThatHasPlayerInTeam(int playerId, IEnumerable<GameweekEntry> gameweekEntries, IEnumerable<User> users)
        {
            return gameweekEntries == null ?
                Enumerable.Empty<string>() :
                gameweekEntries.Where(x => x.Picks.Any(pick => pick.PlayerId == playerId)).Select(x => SlackHandleHelper.GetSlackHandleOrFallback(users, x.EntryName));
        }
    }
}