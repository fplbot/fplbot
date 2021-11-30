using FplBot.Discord.Data;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Discord.Handlers.FplEvents;

public class NearDeadlineHandler :
    IHandleMessages<OneHourToDeadline>,
    IHandleMessages<TwentyFourHoursToDeadline>
{
    private readonly IGuildRepository _teamRepo;

    private readonly ILogger<NearDeadlineHandler> _logger;

    public NearDeadlineHandler(IGuildRepository teamRepo, ILogger<NearDeadlineHandler> logger)
    {
        _teamRepo = teamRepo;
        _logger = logger;
    }

    public async Task Handle(OneHourToDeadline message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Notifying about 60 minutes to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var allGuilds = await _teamRepo.GetAllGuildSubscriptions();
        var text = $"@here ⏳Gameweek {message.GameweekNearingDeadline.Id} deadline in 60 minutes!";
        foreach (var guild in allGuilds)
        {
            if (guild.Subscriptions.ContainsSubscriptionFor(EventSubscription.Deadlines))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishRichToGuildChannel(guild.GuildId, guild.ChannelId, "ℹ️ Deadline", text), options);
            }
        }
    }

    public async Task Handle(TwentyFourHoursToDeadline message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Notifying about 24 hours to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var allGuilds = await _teamRepo.GetAllGuildSubscriptions();
        var text = $"⏳Gameweek {message.GameweekNearingDeadline.Id} deadline in 24 hours!";
        foreach (var guild in allGuilds)
        {
            if (guild.Subscriptions.ContainsSubscriptionFor(EventSubscription.Deadlines))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishRichToGuildChannel(guild.GuildId, guild.ChannelId, "ℹ️ Deadline", text), options);
            }
        }
    }
}
