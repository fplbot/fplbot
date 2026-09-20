using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.EventHandlers.Web;

public static class WebPushFormatter
{
    public static (string Title, string Body) Goal(FixtureScore score, string scorer) =>
        ($"⚽ {score.HomeTeam.ShortName} {score.HomeTeamScore}-{score.AwayTeamScore} {score.AwayTeam.ShortName}",
            $"{scorer} {score.Minutes}'");

    public static (string Title, string Body) FixtureFullTime(FixtureScore score) =>
        ($"🔚 {score.HomeTeam.ShortName} {score.HomeTeamScore}-{score.AwayTeamScore} {score.AwayTeam.ShortName}",
            "Full time");

    public static (string Title, string Body) FixtureRemoved(int count) =>
        ("📅 Fixture postponed", $"{count} fixture(s) removed from the gameweek");

    public static (string Title, string Body) Deadline(string relative) =>
        ("⏰ Deadline coming up", $"The gameweek deadline is {relative}");

    public static (string Title, string Body) PriceChanges(int count) =>
        ("💰 Price changes", $"{count} player(s) changed price");

    public static (string Title, string Body) InjuryUpdates(int count) =>
        ("🤕 Injury update", $"{count} player(s) got a status update");

    public static (string Title, string Body) Lineups(string description) =>
        ("📋 Lineups are out", description);

    public static (string Title, string Body) NewPlayers(int count) =>
        ("🆕 New players", $"{count} player(s) joined the game");

    public static (string Title, string Body) Standings(int gameweek) =>
        ($"🏆 GW{gameweek} standings", "Tap to see where your league ended up");

    public static (string Title, string Body) GameweekStarted(int gameweek) =>
        ($"🎬 GW{gameweek} has started", "Captains and transfers are in");
}
