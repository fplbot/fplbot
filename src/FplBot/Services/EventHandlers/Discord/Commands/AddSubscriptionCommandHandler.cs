using FplBot.Data.Discord;
using FplBot.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord.Commands;

public class AddSubscriptionCommandHandler(IGuildRepository repo, ILogger<AddSubscriptionCommandHandler> logger)
    : IConsumer<ProcessAddSubscriptionCommand>
{
    public async Task Consume(ConsumeContext<ProcessAddSubscriptionCommand> context)
    {
        var command = context.Message;
        try
        {
            var newEventSub = Enum.Parse<EventSubscription>(command.Subscription);
            var newFplEvent = ToFplEvent(newEventSub);

            var installation = await repo.GetInstallation(command.GuildId);
            var existingChannel = installation.GetChannel(command.ChannelId);

            if (existingChannel == null)
            {
                installation.Subscribe(command.ChannelId, [newFplEvent]);
                await repo.Save(installation);
                var created = installation.GetChannel(command.ChannelId)!;
                await RespondChecked(context,
                    $"Added new subscription! Subscriptions:\n{Formatter.BulletPoints(created.Events.Current.Select(ToEventSubscription))}");
                return;
            }

            if (existingChannel.IsSubscribedTo(newFplEvent))
            {
                await Respond(context, "⚠️", $"Already subscribing to {command.Subscription}");
                return;
            }

            installation.Subscribe(command.ChannelId, [newFplEvent]);
            await repo.Save(installation);
            await RespondChecked(context, $"Updated subscriptions:\n{Formatter.BulletPoints(existingChannel.Events.Current.Select(ToEventSubscription))}");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed adding subscription {Subscription} for guild {GuildId}", command.Subscription, command.GuildId);
            await Respond(context, "⚠️ Error", "Something went wrong on my end. Try again in a moment.");
        }
    }

    private static Task RespondChecked(ConsumeContext<ProcessAddSubscriptionCommand> context, string description) =>
        ChannelPermissions.Problem(context.Message.AppPermissions) is { } problem
            ? Respond(context, "⚠️ Saved, but I can't post here yet", $"{problem}\n\n{description}")
            : Respond(context, "✅ Success!", description);

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
    private static EventSubscription ToEventSubscription(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());

    private static async Task Respond(ConsumeContext<ProcessAddSubscriptionCommand> context, string title, string description)
    {
        if (context.Message.InteractionToken is { Length: > 0 } token)
        {
            await context.Publish(new RespondToDiscordInteraction(token, title, description));
        }
    }
}
