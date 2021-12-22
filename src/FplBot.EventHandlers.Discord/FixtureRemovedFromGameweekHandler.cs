using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class FixtureRemovedFromGameweekHandler : IHandleMessages<FixtureRemovedFromGameweek>
{
    private readonly IGuildRepository _guildRepo;
    private readonly ILogger<FixtureRemovedFromGameweekHandler> _logger;

    public FixtureRemovedFromGameweekHandler(IGuildRepository guildRepo, ILogger<FixtureRemovedFromGameweekHandler> logger)
    {
        _guildRepo = guildRepo;
        _logger = logger;
    }

    public async Task Handle(FixtureRemovedFromGameweek message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Fixture removed from gameweek {Message}", message);
        var subs = await _guildRepo.GetAllGuildSubscriptions();

        foreach (var sub in subs)
        {
            if (sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.FixtureRemovedFromGameweek))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                var formattedMsg = new PublishRichToGuildChannel(sub.GuildId,
                    sub.ChannelId,
                    $"❌ Fixture off!",
                    $"{message.RemovedFixture.Home.Name}-{message.RemovedFixture.Away.Name}" +
                    $" has been removed from gameweek {message.Gameweek}!");
                await context.Send(formattedMsg, options);
            }
        }
    }
}
