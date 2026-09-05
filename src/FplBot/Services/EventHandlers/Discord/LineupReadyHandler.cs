using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class LineupReadyHandler : IConsumer<LineupReady>
{
    private readonly IGuildRepository _guildRepository;

    public LineupReadyHandler(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    public async Task Consume(ConsumeContext<LineupReady> context)
    {
        var message = context.Message;
        var subs = await _guildRepository.GetAllGuildSubscriptions();
        var lineups = message.Lineup;
        var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* ";
        var formattedLineup = Formatter.FormatLineup(lineups);
        foreach (var sub in subs)
        {
            if (sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.Lineups))
            {
                await context.Send(new PublishRichToGuildChannel(sub.GuildId, sub.ChannelId, $"ℹ️ {firstMessage}", $"{formattedLineup}"));
            }

        }
    }
}
