using System.Collections.Generic;
using System.Linq;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.GameweekLifecycle
{
    internal class MatchDetailsMapper
    {
        public static LineupReady TryMapToLineup(MatchDetails details)
        {
            try
            {
                var homeTeam = details.Teams.First().Team;
                var awayTeam = details.Teams.Last().Team;

                var homeTeamLineup = details.TeamLists.First(l => l.TeamId == homeTeam.Id);
                var awayTeamLineup = details.TeamLists.First(l => l.TeamId == awayTeam.Id);

                return new LineupReady
                {
                    FixturePulseId = details.Id,
                    HomeTeamNameAbbr = homeTeam.Club.Abbr,
                    AwayTeamNameAbbr = awayTeam.Club.Abbr,
                    HomeTeamLineup = OrderByFormation(homeTeamLineup),
                    AwayTeamLineup = OrderByFormation(awayTeamLineup)
                };
            }
            catch
            {
                return null;
            }
        }

        private static FormationDetails OrderByFormation(LineupContainer teamLineup)
        {
            var p = new List<FormationSegment>();
            foreach (var segment in teamLineup.Formation.Players)
            {
                var playersInSegment = segment.Select(playerId => teamLineup.Lineup.First(p => p.Id == playerId));
                p.Add(new FormationSegment
                {
                    SegmentPosition =  playersInSegment.First().MatchPosition ,
                    PlayersInSegment = playersInSegment.Select(i => new SegmentPlayer { Name = i.Name.ToString()}).ToList()
                });
            }

            return new FormationDetails
            {
                Label = teamLineup.Formation.Label,
                Segments = p
            };
        }
    }
}
