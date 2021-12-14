using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

[TimeToBeReceived("00:15:00")] // discard events not being handled within 15 mins
public record LineupReady(Lineups Lineup) : IEvent;

public record Lineups(int FixturePulseId, FormationDetails HomeTeamLineup, FormationDetails AwayTeamLineup);

public record FormationDetails(string TeamName, string Formation, List<FormationSegment> Segments);

public record FormationSegment(string SegmentPosition, List<SegmentPlayer> PlayersInSegment);

public record SegmentPlayer(string Name, bool Captain);
