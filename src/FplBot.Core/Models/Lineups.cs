using System.Collections.Generic;

namespace FplBot.Core.Models
{
    public class Lineups
    {
        public int FixturePulseId { get; set; }
        public string HomeTeamNameAbbr { get; set; }
        public string AwayTeamNameAbbr { get; set; }

        public FormationDetails  HomeTeamLineup { get; set; }
        public FormationDetails  AwayTeamLineup { get; set; }
    }

    public class FormationDetails
    {
        public string Label { get; set; }
        public List<FormationSegment> Segments { get; set; }
    }

    public class FormationSegment
    {
        public IEnumerable<PlayerInLineup>  PlayersInSegment { get; set; }
        public string SegmentPosition { get; set; }
    }
}