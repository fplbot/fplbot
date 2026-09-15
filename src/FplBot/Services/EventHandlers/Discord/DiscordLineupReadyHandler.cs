using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordLineupReadyHandler(IGuildRepository guildRepository) : IConsumer<LineupReady>
{
    public async Task Consume(ConsumeContext<LineupReady> context)
    {
        var message = context.Message;
        var installations = await guildRepository.GetAllInstallations();
        var lineups = message.Lineup;
        var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* ";
        var formattedLineup = Formatter.FormatLineup(lineups);

        foreach (var installation in installations)
        {
            foreach (var channel in installation.GetSubscriptionsTo(FplEvent.Lineups))
            {
                await context.Publish(new PublishRichToGuildChannel(installation.Id, channel.ChannelId, $"ℹ️ {firstMessage}", $"{formattedLineup}"));
            }
        }
    }
}
