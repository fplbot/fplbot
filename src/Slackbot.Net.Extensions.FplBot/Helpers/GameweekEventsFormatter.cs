using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
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
            ICollection<Team> teams
            )
        {
            var formattedStrings = new List<string>();

            newFixtureEvents.ForEach(newFixtureEvent =>
            {
                newFixtureEvent.StatMap.Keys.ToList().ForEach(statType =>
                {
                    var events = newFixtureEvent.StatMap.FirstOrDefault(k => k.Key == statType).Value;

                    switch (statType)
                    {
                        case StatType.GoalsScored:
                            formattedStrings.Concat(FormatNewGoals(events, players));
                            // Format goals
                            break;
                        case StatType.Assists:
                            // Format assists
                            break;
                        case StatType.OwnGoals:
                            // Format own goals
                            break;
                        case StatType.RedCards:
                            // Format red cards
                            break;
                        case StatType.PenaltiesMissed:
                            // Format penalties missed
                            break;
                        case StatType.PenaltiesSaved:
                            // Format penalties saved
                            break;
                    }
                });
            });

            return formattedStrings;
        }

        private List<string> FormatNewGoals(List<PlayerEvent> newGoalEvents, ICollection<Player> players)
        {
            return newGoalEvents.Select(g =>
            {
                var player = players.Single(x => x.Id == g.PlayerId);

                if (g.IsRemoved)
                {
                    // VAR
                    return null;
                }

                var message = $":soccer: {player.FirstName} {player.SecondName} just scored a goal!";
                return message;

                /*var users = (await slackClient.UsersList())?.Members?.Where(user => user.IsActiveRealPerson()).ToArray();
                if (users == null) return message;

                var entriesTransferredPlayerOut = EntriesThatTransferredPlayerOutThisGameweek(users, player.Id).ToArray();
                if (entriesTransferredPlayerOut.Any())
                {
                    message += $" {string.Format(_transferredGoalScorerOutTaunts.GetRandom(), string.Join(", ", entriesTransferredPlayerOut))}";
                }

                return message;*/
            }).WhereNotNull().ToList();
        }

        private IEnumerable<string> EntriesThatTransferredPlayerOutThisGameweek(User[] users, int playerId, IEnumerable<TransfersByGameWeek.Transfer> transfersForCurrentGameweek)
        {
            return transfersForCurrentGameweek == null ? 
                Enumerable.Empty<string>() : 
                transfersForCurrentGameweek.Where(x => x.PlayerTransferredOut == playerId).Select(x => SlackHandleHelper.GetSlackHandle(users, x.EntryName) ?? x.EntryName);
        }
    }
}