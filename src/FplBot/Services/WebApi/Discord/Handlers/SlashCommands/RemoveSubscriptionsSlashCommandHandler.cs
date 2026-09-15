using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;

namespace FplBot.Discord.Handlers.SlashCommands;

public class RemoveSubscriptionSlashCommandHandler(IGuildRepository repo) : ISlashCommandHandler
{
    public string CommandName => "subscriptions";

    public string SubCommandName => "remove";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var installation = await repo.GetInstallation(context.GuildId);
        var existingChannel = installation.GetChannel(context.ChannelId);

        if (existingChannel == null || !existingChannel.Events.Current.Any())
        {
            return Respond("🤷‍♀️O RLY?", "Did not find any subscription(s) in this channel to remove!");
        }

        EventSubscription eventSub = Enum.Parse<EventSubscription>(context.CommandInput!.Value);
        var events = existingChannel.Events.Current;
        var wasSubscribedToAll = events.Count() == 1 && events.First() == FplEvent.All;

        bool isLastSub = events.Count() == 1 && events.First() == ToFplEvent(eventSub);
        if (existingChannel.FollowedLeagueId == null && (isLastSub || eventSub == EventSubscription.All))
        {
            installation.RemoveChannel(context.ChannelId);
            await repo.Save(installation);
            return Respond("✅ Success!", "Removed subscription to this channel.");
        }

        installation.Unsubscribe(context.ChannelId, ToFplEvent(eventSub));
        await repo.Save(installation);

        if (existingChannel.Events.Current.Any())
        {
            var message = wasSubscribedToAll
                ? $"No longer subscribing to all events. Updated list:\n{Formatter.BulletPoints(existingChannel.Events.Current.Select(ToEventSubscription))}"
                : $"Unsubscribed from {eventSub}. Updated list:\n{Formatter.BulletPoints(existingChannel.Events.Current.Select(ToEventSubscription))}";
            return Respond("✅ Success!", message);
        }
        return Respond("✅ Success!", "No longer subscribing to any events.");
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
    private static EventSubscription ToEventSubscription(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());

    private static ChannelMessageWithSourceEmbedResponse Respond(string title, string content)
    {
        return new ChannelMessageWithSourceEmbedResponse
               {
            Embeds = [new(title, content)]
        };
    }
}
