using FplBot.Messaging.Contracts.Events.v1;

namespace Fpl.EventPublishers.Models.Mappers;

public class MatchDetailsMapper
{
    public static LineupReady TryMapToLineup(MatchDetails details, int fixtureCode, string homeTeamAbbr, string awayTeamAbbr, Action<Exception> logger = null)
    {
        try
        {
            var homeTeamLineup = details.HomeTeam;
            var awayTeamLineup = details.AwayTeam;

            if (homeTeamLineup != null && homeTeamLineup.HasLineups() && awayTeamLineup != null && awayTeamLineup.HasLineups())
            {
                return new LineupReady
                (
                    new Lineups
                    (
                        fixtureCode,
                        OrderByFormation(homeTeamAbbr, homeTeamLineup),
                        OrderByFormation(awayTeamAbbr, awayTeamLineup)
                    )
                );
            }

            return null;
        }
        catch(Exception e)
        {
            logger?.Invoke(e);
            return null;
        }
    }

    private static FormationDetails OrderByFormation(string teamName, TeamLineup teamLineup)
    {
        var p = new List<FormationSegment>();
        foreach (var segment in teamLineup.Formation.Lineup)
        {
            var playersInSegment = segment.Select(playerId => teamLineup.Players.First(p => p.Id == playerId)).ToList();
            p.Add(new FormationSegment
            (
                playersInSegment.First().MatchPosition,
                playersInSegment.Select(i => new SegmentPlayer(i.DisplayName, i.IsCaptain)).ToList()
            ));
        }

        return new FormationDetails(teamName, teamLineup.Formation.Label, p);
    }
}
