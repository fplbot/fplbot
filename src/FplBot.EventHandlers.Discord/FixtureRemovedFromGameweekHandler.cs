using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class FixtureRemovedFromGameweekHandler : IHandleMessages<FixtureRemovedFromGameweek>
{
    private readonly IGuildRepository _guildRepo;

    public FixtureRemovedFromGameweekHandler(IGuildRepository guildRepo)
    {
        _guildRepo = guildRepo;
    }

    public async Task Handle(FixtureRemovedFromGameweek message, IMessageHandlerContext context)
    {
        var subs = await _guildRepo.GetAllGuildSubscriptions();

        foreach (var sub in subs)
        {
            if (sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.FixtureRemoved))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                var formattedMsg = new PublishRichToGuildChannel(sub.GuildId,
                    sub.ChannelId,
                    $"{message.RemovedFixture.HomeTeamShortName}-{message.RemovedFixture.AwayTeamShortName}",
                    $"Fixture has been removed from gameweek {message.Gameweek}");
                await context.Send(formattedMsg);
            }
        }
    }
}
