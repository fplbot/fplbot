using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord.Commands;

public class RemoveSubscriptionCommandHandler(IGuildRepository repo, ILogger<RemoveSubscriptionCommandHandler> logger)
    : IConsumer<ProcessRemoveSubscriptionCommand>
{
    public async Task Consume(ConsumeContext<ProcessRemoveSubscriptionCommand> context)
    {
        var command = context.Message;
        try
        {
            var installation = await repo.GetInstallation(command.GuildId);
            var existingChannel = installation.GetChannel(command.ChannelId);

            if (existingChannel == null || !existingChannel.Events.Current.Any())
            {
                await Respond(context, "🤷‍♀️O RLY?", "Did not find any subscription(s) in this channel to remove!");
                return;
            }

            var eventSub = Enum.Parse<EventSubscription>(command.Subscription);
            var events = existingChannel.Events.Current;
            var wasSubscribedToAll = events.Count() == 1 && events.First() == FplEvent.All;

            bool isLastSub = events.Count() == 1 && events.First() == ToFplEvent(eventSub);
            if (existingChannel.FollowedLeagueId == null && (isLastSub || eventSub == EventSubscription.All))
            {
                installation.RemoveChannel(command.ChannelId);
                await repo.Save(installation);
                await Respond(context, "✅ Success!", "Removed subscription to this channel.");
                return;
            }

            installation.Unsubscribe(command.ChannelId, ToFplEvent(eventSub));
            await repo.Save(installation);

            if (existingChannel.Events.Current.Any())
            {
                var message = wasSubscribedToAll
                    ? $"No longer subscribing to all events. Updated list:\n{Formatter.BulletPoints(existingChannel.Events.Current.Select(ToEventSubscription))}"
                    : $"Unsubscribed from {eventSub}. Updated list:\n{Formatter.BulletPoints(existingChannel.Events.Current.Select(ToEventSubscription))}";
                await Respond(context, "✅ Success!", message);
                return;
            }

            await Respond(context, "✅ Success!", "No longer subscribing to any events.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed removing subscription {Subscription} for guild {GuildId}", command.Subscription, command.GuildId);
            await Respond(context, "⚠️ Error", "Something went wrong on my end. Try again in a moment.");
        }
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
    private static EventSubscription ToEventSubscription(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());

    private static async Task Respond(ConsumeContext<ProcessRemoveSubscriptionCommand> context, string title, string description)
    {
        if (context.Message.InteractionToken is { Length: > 0 } token)
        {
            await context.Publish(new RespondToDiscordInteraction(token, title, description));
        }
    }
}
