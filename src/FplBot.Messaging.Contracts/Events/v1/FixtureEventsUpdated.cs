using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1;

[TimeToBeReceived("00:15:00")] // discard events not being handled within 15 mins
public record FixtureEventsOccured(List<FixtureEvents> FixtureEvents) : IEvent;

public record FixtureEvents(FixtureScore FixtureScore, Dictionary<StatType, List<PlayerEvent>> StatMap);

public record FixtureScore(FixtureTeam HomeTeam, FixtureTeam AwayTeam, int? Minutes, int? HomeTeamScore, int? AwayTeamScore);

public record FixtureTeam(int TeamId, string Name, string ShortName);

public enum StatType
{
    GoalsScored,
    Assists,
    OwnGoals,
    YellowCards,
    RedCards,
    PenaltiesSaved,
    PenaltiesMissed,
    Saves,
    Bonus,
    Unknown
}

public record PlayerEvent(PlayerDetails Player, TeamType Team, bool IsRemoved);

public enum TeamType
{
    Home,
    Away
}

public record PlayerDetails(int Id, string WebName);
