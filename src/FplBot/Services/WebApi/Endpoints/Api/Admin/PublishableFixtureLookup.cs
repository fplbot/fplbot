using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.WebApi.Endpoints.Api.Admin;

// Shared by the Slack, Discord and web push admin "publish now" actions, all of which target
// the first fixture (by kickoff time) in the current gameweek. Kept in one place because the
// fixture-events diff below is a subtle trick (see ResolveFixtureEvents) that all three platforms
// need to get identically right, rather than three independent chances to get it subtly wrong.
internal static class PublishableFixtureLookup
{
    internal static async Task<Fixture?> FindFirstFixture(IFixtureClient fixtureClient, int gameweekId)
    {
        var fixtures = await fixtureClient.GetFixturesByGameweek(gameweekId);
        return fixtures?.OrderBy(f => f.KickOffTime).FirstOrDefault();
    }

    // LiveEventsExtractor.GetUpdatedFixtureEvents is built to diff a fixture's stats against the
    // same fixture's stats from the previous poll — it only reports something for a fixture it
    // already has a prior snapshot of, and only reports what's new since that snapshot, so passing
    // an empty "previous" would report nothing for a fixture it's never seen before. Handing it an
    // otherwise-identical clone with an empty stat sheet makes every currently-recorded stat "new",
    // which is exactly the real current state — not synthetic data, just today's real events
    // replayed as if they'd just been diffed for the first time.
    internal static async Task<(List<FixtureEvents> Events, string? RefusalReason)> ResolveFixtureEvents(
        Fixture fixture, IGlobalSettingsClient gameweekClient)
    {
        var settings = await gameweekClient.GetGlobalSettings();
        var players = settings?.Players ?? [];
        var teams = settings?.Teams ?? [];

        var emptyStatsClone = new Fixture
        {
            Id = fixture.Id,
            Code = fixture.Code,
            HomeTeamId = fixture.HomeTeamId,
            AwayTeamId = fixture.AwayTeamId,
            Minutes = fixture.Minutes,
            Stats = []
        };

        var events = LiveEventsExtractor.GetUpdatedFixtureEvents([fixture], [emptyStatsClone], players, teams).ToList();
        return events.Count == 0
            ? ([], "No goals, assists, cards or penalty misses recorded yet for this fixture.")
            : (events, null);
    }
}
