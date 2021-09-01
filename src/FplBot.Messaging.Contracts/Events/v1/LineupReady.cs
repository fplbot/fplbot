using System.Collections.Generic;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public class LineupReady : IEvent
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
        public List<SegmentPlayer>  PlayersInSegment { get; set; }
        public string SegmentPosition { get; set; }
    }

    public class SegmentPlayer
    {
        public const string MatchPositionGoalie = "G";
        public const string MatchPositionDefender = "D";
        public const string MatchPositionMidfielder = "M";
        public const string MatchPositionForward = "F";

        public int Id { get; set; }
        public string MatchPosition { get; set; }
        public string Name { get; set; }
        public bool Captain { get; set; }
    }
}
