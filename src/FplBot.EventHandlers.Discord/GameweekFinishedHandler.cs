using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class GameweekFinishedHandler : IHandleMessages<GameweekFinished>,
    IHandleMessages<PublishGameweekFinishedToGuild>
{
    private readonly IGuildRepository _repo;
    private readonly ILogger<GameweekFinishedHandler> _logger;
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly ILeagueClient _leagueClient;

    public GameweekFinishedHandler(IGuildRepository repo,
        ILogger<GameweekFinishedHandler> logger,
        IGlobalSettingsClient settingsClient,
        ILeagueClient leagueClient)
    {
        _repo = repo;
        _logger = logger;
        _settingsClient = settingsClient;
        _leagueClient = leagueClient;
    }

    public async Task Handle(GameweekFinished message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Gameweek {message.FinishedGameweek.Id} finished");
        var allSubs = await _repo.GetAllGuildSubscriptions();
        foreach (var sub in allSubs)
        {
            var options = new SendOptions();
            options.RequireImmediateDispatch();
            options.RouteToThisEndpoint();
            await context.Send(new PublishGameweekFinishedToGuild(sub.GuildId, sub.ChannelId, sub.LeagueId, message.FinishedGameweek.Id), options);
        }
    }

    public async Task Handle(PublishGameweekFinishedToGuild message, IMessageHandlerContext context)
    {
        var sub = await _repo.GetGuildSubscription(message.GuildId, message.ChannelId);

        if (sub != null && message.LeagueId.HasValue && sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.Standings))
        {
            var settings = await _settingsClient.GetGlobalSettings();
            var gameweeks = settings.Gameweeks;
            var gw = gameweeks.SingleOrDefault(g => g.Id == message.GameweekId);
            ClassicLeague league = await _leagueClient.GetClassicLeague(message.LeagueId.Value, tolerate404:true);
            if (league != null)
            {
                var messages = new List<RichMesssage>();
                var intro = Formatter.FormatGameweekFinished(gw, league);
                var standings = Formatter.GetStandings(league, gw, includeExternalLinks:false);
                var topThree = Formatter.GetTopThreeGameweekEntries(league, gw,includeExternalLinks:false);
                var worst = Formatter.GetWorstGameweekEntry(league, gw, includeExternalLinks:false);
                messages.AddRange(new RichMesssage[]
                {
                    new ("ℹ️ Gameweek finished!",intro),
                    new ("ℹ️ Standings", standings),
                    new ("ℹ️ Top 3", topThree),
                    new ("ℹ️ Lantern beige", worst)
                });
                var i = 0;
                foreach (var richMessage in messages)
                {
                    i = i + 2;
                    var sendOptions = new SendOptions();
                    sendOptions.DelayDeliveryWith(TimeSpan.FromSeconds(i));
                    sendOptions.RouteToThisEndpoint();
                    await context.Send(new PublishRichToGuildChannel(message.GuildId, message.ChannelId, richMessage.Title, richMessage.Description), sendOptions);
                }
            }
            else
            {
                string msg = $"Standings are now generally ready, but you're subscribing to a non-classic or " +
                             $"non-existing classic FPL league: '{message.LeagueId}'";
                await context.SendLocal(new PublishRichToGuildChannel(message.GuildId, message.ChannelId, "⚠️ Standings ready", msg));
            }
        }
    }
}
