using Fpl.Client.Abstractions;
using FplBot.Data.Web;
using FplBot.Domain;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.FixtureStats;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Web;

public class WebPushDispatchHandler(
    IWebPushSubscriberRepository repo,
    IGlobalSettingsClient settingsClient,
    IFixtureClient fixtureClient,
    ILiveClient liveClient,
    ILeagueClient leagueClient,
    ICaptainsByGameWeek captainsByGameweek,
    ITransfersByGameWeek transfersByGameweek,
    ILogger<WebPushDispatchHandler> logger) :
    IConsumer<InjuryUpdateOccured>,
    IConsumer<PlayersPriceChanged>,
    IConsumer<PlayersLikelyToChangePrice>,
    IConsumer<TwentyFourHoursToDeadline>,
    IConsumer<OneHourToDeadline>,
    IConsumer<LineupReady>,
    IConsumer<NewPlayersRegistered>,
    IConsumer<FixtureEventsOccured>,
    IConsumer<FixtureFinished>,
    IConsumer<FixtureRemovedFromGameweek>,
    IConsumer<GameweekFinished>,
    IConsumer<ProcessGameweekFinishedForWebPushSubscriber>,
    IConsumer<GameweekJustBegan>,
    IConsumer<ProcessGameweekStartedForWebPushSubscriber>
{
    private const int MemberCountForLargeLeague = 25;

    public Task Consume(ConsumeContext<InjuryUpdateOccured> context)
    {
        var relevant = context.Message.PlayersWithInjuryUpdates.Where(c => c.Player.IsRelevant()).ToList();
        return relevant.Count == 0
            ? Task.CompletedTask
            : Dispatch(context, FplEvent.InjuryUpdates, ("🤕 Injury update", Formatter.FormatInjuryStatusUpdates(relevant, markdown: false)));
    }

    public Task Consume(ConsumeContext<PlayersPriceChanged> context)
    {
        var relevant = context.Message.PlayersWithPriceChanges.Where(c => c.IsRelevant()).ToList();
        return relevant.Count == 0
            ? Task.CompletedTask
            : Dispatch(context, FplEvent.PriceChanges, ("💰 Price changes", Formatter.FormatPriceChanged(relevant, markdown: false)));
    }

    public Task Consume(ConsumeContext<PlayersLikelyToChangePrice> context)
    {
        var players = context.Message.Players;
        return players.Count == 0
            ? Task.CompletedTask
            : Dispatch(context, FplEvent.LikelyPriceChanges, ("🔮 Likely price changes", Formatter.FormatLikelyPriceChanges(players, markdown: false)));
    }

    public Task Consume(ConsumeContext<TwentyFourHoursToDeadline> context) =>
        Dispatch(context, FplEvent.Deadlines, WebPushMessages.DeadlineReminder(context.Message.GameweekNearingDeadline.Id, "in 24 hours"));

    public Task Consume(ConsumeContext<OneHourToDeadline> context) =>
        Dispatch(context, FplEvent.Deadlines, WebPushMessages.DeadlineReminder(context.Message.GameweekNearingDeadline.Id, "in 60 minutes"));

    public Task Consume(ConsumeContext<LineupReady> context) =>
        Dispatch(context, FplEvent.Lineups, WebPushMessages.Lineups(context.Message.Lineup));

    public Task Consume(ConsumeContext<NewPlayersRegistered> context)
    {
        var relevant = context.Message.NewPlayers.Where(c => c.IsRelevant()).ToList();
        if (relevant.Count == 0)
        {
            return Task.CompletedTask;
        }

        var title = relevant.Count > 1 ? "🆕 New players" : "🆕 New player";
        return Dispatch(context, FplEvent.NewPlayers, (title, Formatter.FormatNewPlayers(relevant, includeheader: false)));
    }

    public async Task Consume(ConsumeContext<FixtureEventsOccured> context)
    {
        foreach (var fixtureEvents in context.Message.FixtureEvents)
        {
            var statsBySubscriber = new Dictionary<WebPushSubscriberId, HashSet<StatType>>();

            foreach (var statType in fixtureEvents.StatMap.Keys)
            {
                if (GetFplEventForStat(statType) is not { } fplEvent)
                {
                    continue;
                }

                foreach (var subscriberId in await repo.GetSubscribedTo(fplEvent))
                {
                    if (!statsBySubscriber.TryGetValue(subscriberId, out var stats))
                    {
                        statsBySubscriber[subscriberId] = stats = [];
                    }

                    stats.Add(statType);
                }
            }

            foreach (var (subscriberId, stats) in statsBySubscriber)
            {
                var messages = GameweekEventsFormatter.FormatNewFixtureEvents([fixtureEvents], stats.Contains, FormattingType.Web);
                foreach (var message in messages)
                {
                    await context.Publish(new PublishToWebPushSubscriber(subscriberId.Value, message.Title, message.Details, null));
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
        var liveItems = finishedFixture.Event.HasValue
            ? await liveClient.GetLiveItems(finishedFixture.Event.Value, isOngoingGameweek: true)
            : null;
        var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(settings?.Teams ?? [], settings?.Players ?? [], finishedFixture, liveItems);
        await Dispatch(context, FplEvent.FixtureFullTime, WebPushMessages.FixtureFullTime(finished));
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
        Dispatch(context, FplEvent.FixtureRemovedFromGameweek,
            ("📅 Fixture postponed", Formatter.FormatFixtureRemoved(context.Message.RemovedFixture, context.Message.Gameweek)));

    public async Task Consume(ConsumeContext<GameweekFinished> context)
    {
        foreach (var (id, leagueId) in await repo.GetFollowingALeague(FplEvent.Standings))
        {
            await context.Publish(new ProcessGameweekFinishedForWebPushSubscriber(id.Value, (int)leagueId.Value, context.Message.FinishedGameweek.Id));
        }
    }

    public async Task Consume(ConsumeContext<ProcessGameweekFinishedForWebPushSubscriber> context)
    {
        var message = context.Message;
        var settings = await settingsClient.GetGlobalSettings();
        var gw = (settings?.Gameweeks ?? []).SingleOrDefault(g => g.Id == message.GameweekId);
        var league = await leagueClient.GetClassicLeague(message.LeagueId, tolerate404: true);

        if (league is null || gw is null)
        {
            var msg = $"Standings are now generally ready, but you're following a non-classic or non-existing classic FPL league: '{message.LeagueId}'";
            await context.Publish(new PublishToWebPushSubscriber(message.SubscriberId, "⚠️ Standings ready", msg, message.LeagueId));
            return;
        }

        if (league.Properties?.StartEvent is var startEvent && message.GameweekId >= startEvent)
        {
            var intro = Formatter.FormatGameweekFinished(gw, league, includeTitle: false, markdown: false);
            var topThree = Formatter.GetTopThreeGameweekEntries(league, gw, includeExternalLinks: false, includeIntro: false);
            var standings = Formatter.GetStandingsDiscord(league, gw, includeExternalLinks: false);
            var worst = league.Standings?.HasNext == true ? null : Formatter.GetWorstGameweekEntry(league, gw, includeExternalLinks: false);

            var body = string.Join("\n\n", new[] { intro, topThree, standings, worst }.Where(s => !string.IsNullOrWhiteSpace(s)));
            await context.Publish(new PublishToWebPushSubscriber(message.SubscriberId, "🏆 Gameweek finished!", body, message.LeagueId));
        }
    }

    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        foreach (var (id, leagueId) in await repo.GetFollowingALeague(FplEvent.Captains, FplEvent.Transfers))
        {
            await context.Publish(new ProcessGameweekStartedForWebPushSubscriber(id.Value, (int)leagueId.Value, context.Message.NewGameweek.Id));
        }
    }

    public async Task Consume(ConsumeContext<ProcessGameweekStartedForWebPushSubscriber> context)
    {
        var message = context.Message;
        if (await repo.Find(new WebPushSubscriberId(message.SubscriberId)) is not { } subscriber)
        {
            return;
        }

        var league = await leagueClient.GetClassicLeague(message.LeagueId, tolerate404: true);
        var leagueStarted = league?.Properties?.StartEvent is var startEvent && message.GameweekId >= startEvent;
        if (league?.Standings is not { } standings || !leagueStarted)
        {
            return;
        }

        var sections = new List<string>();
        if (subscriber.IsSubscribedTo(FplEvent.Captains))
        {
            var captainPicks = await captainsByGameweek.GetEntryCaptainPicks(message.GameweekId, message.LeagueId);
            sections.Add(standings.Entries.Count < MemberCountForLargeLeague
                ? captainsByGameweek.GetCaptainsByGameWeek(message.GameweekId, captainPicks, includeExternalLinks: false, markdown: false)
                : captainsByGameweek.GetCaptainsStatsByGameWeek(captainPicks, includeHeader: false));
        }

        if (subscriber.IsSubscribedTo(FplEvent.Transfers))
        {
            sections.Add(standings.Entries.Count < MemberCountForLargeLeague
                ? await transfersByGameweek.GetTransfersByGameweekTexts(message.GameweekId, message.LeagueId, includeExternalLinks: false)
                : $"See https://www.fplbot.app/leagues/{message.LeagueId} for all transfers");
        }

        if (sections.Count == 0)
        {
            return;
        }

        var body = string.Join("\n\n", sections);
        await context.Publish(new PublishToWebPushSubscriber(message.SubscriberId, $"🎬 GW{message.GameweekId} has started", body, message.LeagueId));
    }

    private Task Dispatch(ConsumeContext context, FplEvent fplEvent, (string Title, string Body) text) =>
        Dispatch(context, [fplEvent], text);

    private async Task Dispatch(ConsumeContext context, FplEvent[] fplEvents, (string Title, string Body) text)
    {
        foreach (var id in await repo.GetSubscribedTo(fplEvents))
        {
            await context.Publish(new PublishToWebPushSubscriber(id.Value, text.Title, text.Body, null));
        }
    }
}
