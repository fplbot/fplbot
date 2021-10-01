using System.Collections.Generic;
using System.Linq;
using FplBot.Messaging.Contracts.Events.v1;

namespace Fpl.Workers.Models.Mappers
{
    public class MatchDetailsMapper
    {
        public static LineupReady TryMapToLineup(MatchDetails details)
        {
            try
            {
                var homeTeam = details.Teams.First().Team;
                var awayTeam = details.Teams.Last().Team;

                var homeTeamLineup = details.TeamLists.FirstOrDefault(l => l.TeamId == homeTeam.Id);
                var awayTeamLineup = details.TeamLists.FirstOrDefault(l => l.TeamId == awayTeam.Id);

                if (homeTeamLineup != null && homeTeamLineup.HasLineups() && awayTeamLineup != null && awayTeamLineup.HasLineups())
                {
                    return new LineupReady
                    (
                        new Lineups
                        (
                            details.Id,
                            OrderByFormation(homeTeam.Club.Abbr, homeTeamLineup),
                            OrderByFormation(awayTeam.Club.Abbr, awayTeamLineup)
                        )
                    );
                }

                return null;

            }
            catch
            {
                return null;
            }
        }

        private static FormationDetails OrderByFormation(string teamName, LineupContainer teamLineup)
        {
            var p = new List<FormationSegment>();
            foreach (var segment in teamLineup.Formation.Players)
            {
                var playersInSegment = segment.Select(playerId => teamLineup.Lineup.First(p => p.Id == playerId));
                p.Add(new FormationSegment
                (
                    playersInSegment.First().MatchPosition ,
                    playersInSegment.Select(i => new SegmentPlayer
                    (
                        i.Name.ToString(),
                        i.Captain
                    )).ToList()
                ));
            }

            return new FormationDetails
            (
                teamName,
                teamLineup.Formation.Label,
                p
            );
        }
    }
}
