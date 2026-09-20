using FplBot.Formatting;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.EventHandlers.Web;

// Shared by the real dispatch handler (WebPushDispatchHandler) and the admin "publish now" tool
// (AdminWebPushEndpoints) so the two can never drift apart on title/body wording for the same event.
public static class WebPushMessages
{
    public static (string Title, string Body) Lineups(Lineups lineup) =>
        ($"📋 Lineups {lineup.HomeTeamLineup.TeamName}-{lineup.AwayTeamLineup.TeamName}", Formatter.FormatLineup(lineup, markdown: false));

    public static (string Title, string Body) FixtureFullTime(FinishedFixture finished) =>
        ($"🔚 {finished.HomeTeam.ShortName} {finished.Fixture.HomeTeamScore}-{finished.Fixture.AwayTeamScore} {finished.AwayTeam.ShortName}",
            Formatter.FormatProvisionalFinished(finished));

    public static (string Title, string Body) DeadlineReminder(int gameweekId, string relative) =>
        ("⏰ Deadline coming up", Formatter.FormatDeadlineReminder(gameweekId, relative));
}
