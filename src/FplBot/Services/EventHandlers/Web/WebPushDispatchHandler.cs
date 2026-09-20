using Fpl.Client.Abstractions;
using FplBot.Data.Web;
using FplBot.Domain;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Web;

public class WebPushDispatchHandler(
    IWebPushSubscriberRepository repo,
    IGlobalSettingsClient settingsClient,
    IFixtureClient fixtureClient,
    ILogger<WebPushDispatchHandler> logger) :
    IConsumer<InjuryUpdateOccured>,
    IConsumer<PlayersPriceChanged>,
    IConsumer<TwentyFourHoursToDeadline>,
    IConsumer<OneHourToDeadline>,
    IConsumer<LineupReady>,
    IConsumer<NewPlayersRegistered>,
    IConsumer<FixtureEventsOccured>,
    IConsumer<FixtureFinished>,
    IConsumer<FixtureRemovedFromGameweek>,
    IConsumer<GameweekFinished>,
    IConsumer<GameweekJustBegan>
{
    public Task Consume(ConsumeContext<InjuryUpdateOccured> context)
    {
        var relevant = context.Message.PlayersWithInjuryUpdates.Where(c => c.Player.IsRelevant()).ToList();
        return relevant.Count == 0
            ? Task.CompletedTask
            : DispatchGlobal(context, FplEvent.InjuryUpdates, WebPushFormatter.InjuryUpdates(relevant.Count));
    }

    public Task Consume(ConsumeContext<PlayersPriceChanged> context)
    {
        var relevant = context.Message.PlayersWithPriceChanges.Where(c => c.IsRelevant()).ToList();
        return relevant.Count == 0
            ? Task.CompletedTask
            : DispatchGlobal(context, FplEvent.PriceChanges, WebPushFormatter.PriceChanges(relevant.Count));
    }

    public Task Consume(ConsumeContext<TwentyFourHoursToDeadline> context) =>
        DispatchGlobal(context, FplEvent.Deadlines, WebPushFormatter.Deadline("in 24 hours"));

    public Task Consume(ConsumeContext<OneHourToDeadline> context) =>
        DispatchGlobal(context, FplEvent.Deadlines, WebPushFormatter.Deadline("in 60 minutes"));

    public Task Consume(ConsumeContext<LineupReady> context) =>
        DispatchGlobal(context, FplEvent.Lineups,
            WebPushFormatter.Lineups(
                $"{context.Message.Lineup.HomeTeamLineup.TeamName} v {context.Message.Lineup.AwayTeamLineup.TeamName}"));

    public Task Consume(ConsumeContext<NewPlayersRegistered> context)
    {
        var relevant = context.Message.NewPlayers.Where(c => c.IsRelevant()).ToList();
        return relevant.Count == 0
            ? Task.CompletedTask
            : DispatchGlobal(context, FplEvent.NewPlayers, WebPushFormatter.NewPlayers(relevant.Count));
    }

    public async Task Consume(ConsumeContext<FixtureEventsOccured> context)
    {
        foreach (var fixtureEvents in context.Message.FixtureEvents)
        {
            foreach (var (statType, events) in fixtureEvents.StatMap)
            {
                if (GetFplEventForStat(statType) is not { } fplEvent)
                {
                    continue;
                }

                foreach (var playerEvent in events.Where(e => !e.IsRemoved))
                {
                    await DispatchGlobal(context, fplEvent,
                        WebPushFormatter.FixtureEvent(statType, fixtureEvents.FixtureScore, playerEvent.Player.WebName));
                }
            }
        }
    }

    public async Task Consume(ConsumeContext<FixtureFinished> context)
    {
        var fixtures = await fixtureClient.GetFixtures() ?? [];
        if (fixtures.FirstOrDefault(f => f.Id == context.Message.FixtureId) is not { } finishedFixture)
        {
            logger.LogWarning("Could not find fixture {FixtureId} in FPL API", context.Message.FixtureId);
            return;
        }

        var settings = await settingsClient.GetGlobalSettings();
        var teams = settings?.Teams ?? [];
        await DispatchGlobal(context, FplEvent.FixtureFullTime,
            WebPushFormatter.FixtureFullTime(
                teams.First(t => t.Id == finishedFixture.HomeTeamId).ShortName,
                finishedFixture.HomeTeamScore,
                finishedFixture.AwayTeamScore,
                teams.First(t => t.Id == finishedFixture.AwayTeamId).ShortName));
    }

    private static FplEvent? GetFplEventForStat(StatType statType) => statType switch
    {
        StatType.GoalsScored => FplEvent.FixtureGoals,
        StatType.Assists => FplEvent.FixtureAssists,
        StatType.OwnGoals => FplEvent.FixtureGoals,
        StatType.RedCards => FplEvent.FixtureCards,
        StatType.PenaltiesSaved => FplEvent.FixturePenaltyMisses,
        StatType.PenaltiesMissed => FplEvent.FixturePenaltyMisses,
        _ => null
    };

    public Task Consume(ConsumeContext<FixtureRemovedFromGameweek> context) =>
        DispatchGlobal(context, FplEvent.FixtureRemovedFromGameweek, WebPushFormatter.FixtureRemoved(1));

    public Task Consume(ConsumeContext<GameweekFinished> context) =>
        DispatchWithLeague(context, FplEvent.Standings,
            WebPushFormatter.Standings(context.Message.FinishedGameweek.Id));

    public Task Consume(ConsumeContext<GameweekJustBegan> context) =>
        DispatchWithLeague(context, FplEvent.Captains, WebPushFormatter.GameweekStarted(context.Message.NewGameweek.Id));

    private async Task DispatchGlobal(ConsumeContext context, FplEvent fplEvent, (string Title, string Body) text)
    {
        foreach (var id in await repo.GetSubscribedTo(fplEvent))
        {
            await context.Publish(new PublishToWebPushSubscriber(id.Value, text.Title, text.Body, null));
        }
    }

    private async Task DispatchWithLeague(ConsumeContext context, FplEvent fplEvent, (string Title, string Body) text)
    {
        foreach (var (id, leagueId) in await repo.GetFollowingALeague(fplEvent))
        {
            await context.Publish(new PublishToWebPushSubscriber(id.Value, text.Title, text.Body, (int)leagueId.Value));
        }
    }
}
