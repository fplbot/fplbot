using FplBot.Data.Web;
using FplBot.Domain;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Web;

public class WebPushDispatchHandler(IWebPushSubscriberRepository repo) :
    IConsumer<InjuryUpdateOccured>,
    IConsumer<PlayersPriceChanged>,
    IConsumer<TwentyFourHoursToDeadline>,
    IConsumer<OneHourToDeadline>,
    IConsumer<LineupReady>,
    IConsumer<NewPlayersRegistered>,
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

    public Task Consume(ConsumeContext<PlayersPriceChanged> context) =>
        DispatchGlobal(context, FplEvent.PriceChanges,
            WebPushFormatter.PriceChanges(context.Message.PlayersWithPriceChanges.Count));

    public Task Consume(ConsumeContext<TwentyFourHoursToDeadline> context) =>
        DispatchGlobal(context, FplEvent.Deadlines, WebPushFormatter.Deadline("in 24 hours"));

    public Task Consume(ConsumeContext<OneHourToDeadline> context) =>
        DispatchGlobal(context, FplEvent.Deadlines, WebPushFormatter.Deadline("in 60 minutes"));

    public Task Consume(ConsumeContext<LineupReady> context) =>
        DispatchGlobal(context, FplEvent.Lineups,
            WebPushFormatter.Lineups(
                $"{context.Message.Lineup.HomeTeamLineup.TeamName} v {context.Message.Lineup.AwayTeamLineup.TeamName}"));

    public Task Consume(ConsumeContext<NewPlayersRegistered> context) =>
        DispatchGlobal(context, FplEvent.NewPlayers,
            WebPushFormatter.NewPlayers(context.Message.NewPlayers.Count));

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
