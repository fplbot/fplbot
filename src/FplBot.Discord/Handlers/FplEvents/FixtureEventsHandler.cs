using FplBot.Discord.Data;
using FplBot.Formatting.FixtureStats;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Discord.Handlers.FplEvents;

public class FixtureEventsHandler : IHandleMessages<FixtureEventsOccured>, IHandleMessages<PublishFixtureEventsToGuild>
{
    private readonly IGuildRepository _repo;
    private readonly ILogger<FixtureEventsHandler> _logger;

    public FixtureEventsHandler(IGuildRepository repo, ILogger<FixtureEventsHandler> logger)
    {
        _repo = repo;
        _logger = logger;
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
            var eventMessages = GameweekEventsFormatter.FormatNewFixtureEvents(message.FixtureEvents, sub.Subscriptions.ContainsStat, FormattingType.Discord);
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
