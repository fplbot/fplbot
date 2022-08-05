using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class NewPlayersHandler : IHandleMessages<NewPlayersRegistered>, IHandleMessages<PremiershipPlayerTransferred>
{
    private readonly IGuildRepository _repo;
    private readonly ILogger<InjuryUpdateHandler> _logger;

    public NewPlayersHandler(IGuildRepository repo, ILogger<InjuryUpdateHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Handle(NewPlayersRegistered message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Handling {message.NewPlayers.Count()} new players");

        var filtered = message.NewPlayers.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var guildSubs = await _repo.GetAllGuildSubscriptions();
            var formatted = Formatter.FormatNewPlayers(filtered);

            foreach (var guildSub in guildSubs)
            {
                if (guildSub.Subscriptions.ContainsSubscriptionFor(EventSubscription.NewPlayers) && !string.IsNullOrEmpty(formatted))
                {
                    var options = new SendOptions();
                    options.RequireImmediateDispatch();
                    options.RouteToThisEndpoint();
                    await context.Send(new PublishRichToGuildChannel(guildSub.GuildId, guildSub.ChannelId,"ℹ️ New players", formatted), options);
                }
            }
        }
        else
        {
            _logger.LogInformation("All new players irrelevant, so not sending any notification");
        }
    }

    public async Task Handle(PremiershipPlayerTransferred message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Handling {message.Transfers.Count()} new transfers");
        var guildSubs = await _repo.GetAllGuildSubscriptions();
        var formatted = Formatter.FormatTransferredPlayers(message.Transfers, includeheader:false);

        foreach (var guildSub in guildSubs)
        {
            if (guildSub.Subscriptions.ContainsSubscriptionFor(EventSubscription.NewPlayers) && !string.IsNullOrEmpty(formatted))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishRichToGuildChannel(guildSub.GuildId, guildSub.ChannelId,"🔄️ Transfer!", formatted), options);
            }
        }
    }
}
