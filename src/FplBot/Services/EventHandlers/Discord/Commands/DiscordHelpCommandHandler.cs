using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord.Commands;

public class DiscordHelpCommandHandler(
    IGuildRepository repo,
    ILeagueClient client,
    ILogger<DiscordHelpCommandHandler> logger)
    : IConsumer<ProcessDiscordHelpCommand>
{
    public async Task Consume(ConsumeContext<ProcessDiscordHelpCommand> context)
    {
        var command = context.Message;
        try
        {
            await context.Publish(new RespondToDiscordInteraction(command.InteractionToken, "ℹ️ HELP", await BuildContent(command)));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed building help for guild {GuildId}", command.GuildId);
            await context.Publish(new RespondToDiscordInteraction(command.InteractionToken, "⚠️ Error", "Something went wrong on my end. Try again in a moment."));
        }
    }

    private async Task<string> BuildContent(ProcessDiscordHelpCommand command)
    {
        var installation = await repo.GetInstallation(command.GuildId);
        var sub = installation.GetChannel(command.ChannelId);
        if (sub == null)
        {
            return "⚠️ Not subscribing to any events. Add one to get notifications!";
        }

        var content = "";
        if (sub.FollowedLeagueId is { } leagueId)
        {
            var league = await client.GetClassicLeague((int)leagueId.Value, tolerate404: true);
            if (league != null)
                content += $"\n**League:**\nCurrently following the '{league.Properties?.Name}' league";
        }
        else
        {
            content += "\n ⚠️ Not following any FPL leagues";
        }

        var allTypes = EventSubscriptionHelper.GetAllSubscriptionTypes();
        var currentSubs = sub.Events.Current.Select(ToEventSubscription).ToList();
        if (currentSubs.Count != 0)
        {
            content += $"\n\n**Subscriptions:**\n{string.Join("\n", currentSubs.Select(s => $" ✅ {s}"))}";

            if (!currentSubs.Contains(EventSubscription.All))
            {
                var allTypesExceptSubs = allTypes.Except(currentSubs).Except([EventSubscription.All]);
                content += $"\n\n**Not subscribing:**\n{string.Join("\n", allTypesExceptSubs.Select(s => $" ❌ {s}"))}";
            }
        }
        else
        {
            content += "\n\n**Subscriptions:**\n ⚠️ No subscriptions";
            content += $"\n\n**Events you may subscribe to:**\n{string.Join("\n", allTypes.Select(s => $" ▪️ {s}"))}";
        }

        return content;
    }

    private static EventSubscription ToEventSubscription(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
