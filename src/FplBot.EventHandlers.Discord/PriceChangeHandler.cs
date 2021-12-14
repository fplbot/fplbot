using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class PriceChangeHandler : IHandleMessages<PlayersPriceChanged>
{

    private readonly IGuildRepository _repo;
    private readonly ILogger<PriceChangeHandler> _logger;

    public PriceChangeHandler(IGuildRepository repo, ILogger<PriceChangeHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Handle(PlayersPriceChanged notification, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Handling {notification.PlayersWithPriceChanges.Count()} price updates");
        var guildSubs = await _repo.GetAllGuildSubscriptions();
        var filtered = notification.PlayersWithPriceChanges.Where(c => c.IsRelevant());

        if (filtered.Any())
        {
            var formatted = Formatter.FormatPriceChanged(filtered);

            foreach (var guildSub in guildSubs)
            {
                if (guildSub.Subscriptions.ContainsSubscriptionFor(EventSubscription.PriceChanges) && !string.IsNullOrEmpty(formatted))
                {
                    var options = new SendOptions();
                    options.RequireImmediateDispatch();
                    options.RouteToThisEndpoint();
                    await context.Send(new PublishRichToGuildChannel(guildSub.GuildId, guildSub.ChannelId, "ℹ️ Price changes", formatted), options);
                }
            }
        }
    }
}
