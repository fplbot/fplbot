using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.FixtureStats;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class FixtureEventsHandler : IHandleMessages<FixtureEventsOccured>, IHandleMessages<PublishFixtureEventsToGuild>
{
    private readonly IGuildRepository _repo;
    private readonly ILogger<FixtureEventsHandler> _logger;
    private readonly IGlobalSettingsClient _globalSettingsClient;
    private readonly ILeagueEntriesByGameweek _leagueEntriesByGameweek;
    private readonly ITransfersByGameWeek _transfersByGameWeek;

    public FixtureEventsHandler(IGuildRepository repo, ILogger<FixtureEventsHandler> logger, IGlobalSettingsClient globalSettingsClient, ILeagueEntriesByGameweek leagueEntriesByGameweek, ITransfersByGameWeek transfersByGameWeek)
    {
        _repo = repo;
        _logger = logger;
        _globalSettingsClient = globalSettingsClient;
        _leagueEntriesByGameweek = leagueEntriesByGameweek;
        _transfersByGameWeek = transfersByGameWeek;
    }

    public async Task Handle(FixtureEventsOccured message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Handling {message.FixtureEvents.Count} new fixture events");
        var subs = await _repo.GetAllGuildSubscriptions();

        foreach (var sub in subs)
        {
            var options = new SendOptions();
            options.RequireImmediateDispatch();
            options.RouteToThisEndpoint();
            await context.Send(new PublishFixtureEventsToGuild(sub.GuildId, sub.ChannelId, message.FixtureEvents), options);
        }
    }

    public async Task Handle(PublishFixtureEventsToGuild message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Publishing {message.FixtureEvents.Count} fixture events to {message.GuildId} and {message.ChannelId}");
        var sub = await _repo.GetGuildSubscription(message.GuildId, message.ChannelId);
        if (sub != null)
        {
            TauntData tauntData = null;
            if (sub.LeagueId.HasValue && sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.Taunts))
            {
                var gws = await _globalSettingsClient.GetGlobalSettings();
                var currentGw = gws.Gameweeks.GetCurrentGameweek();
                IEnumerable<GameweekEntry> entries = new List<GameweekEntry>();
                IEnumerable<TransfersByGameWeek.Transfer> transfers = new List<TransfersByGameWeek.Transfer>();
                if (currentGw != null)
                {
                    entries = await _leagueEntriesByGameweek.GetEntriesForGameweek(currentGw.Id, sub.LeagueId.Value);
                    transfers = await _transfersByGameWeek.GetTransfersByGameweek(currentGw.Id, sub.LeagueId.Value);
                }

                tauntData = new TauntData(transfers, entries);
            }
            var eventMessages = GameweekEventsFormatter.FormatNewFixtureEvents(message.FixtureEvents, sub.Subscriptions.ContainsStat, FormattingType.Discord, tauntData);
            var i = 0;
            foreach (var eventMsg in eventMessages)
            {
                i += 2;
                var sendOptions = new SendOptions();
                sendOptions.DelayDeliveryWith(TimeSpan.FromSeconds(i));
                sendOptions.RouteToThisEndpoint();

                await context.Send(new PublishRichToGuildChannel(message.GuildId, message.ChannelId, eventMsg.Title, eventMsg.Details), sendOptions);
            }
        }
        else
        {
            _logger.LogInformation($"Guild {message.GuildId} in channel {message.ChannelId} not subbing to fixture events. Not sending");
        }
    }
}
