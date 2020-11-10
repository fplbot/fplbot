using System.Linq;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    public class MatchDetailsMapper
    {
        public static Lineups ToLineup(MatchDetails details)
        {
            var homeTeam = details.Teams.First().Team;
            var awayTeam = details.Teams.Last().Team;

            return new Lineups
            {
                FixturePulseId = details.Id,
                HomeTeamNameAbbr = homeTeam.Club.Abbr,
                AwayTeamNameAbbr = awayTeam.Club.Abbr,
                HomeTeamLineup = details.TeamLists.First(l => l.TeamId == homeTeam.Id).Lineup,
                AwayTeamLineup = details.TeamLists.First(l => l.TeamId == awayTeam.Id).Lineup
            };
        }
    }
}