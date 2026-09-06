using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Data.Discord;
using FplBot.Discord.Extensions;
using FplBot.Formatting;

namespace FplBot.Discord.Handlers.SlashCommands;

public class RemoveSubscriptionSlashCommandHandler(IGuildRepository repo) : ISlashCommandHandler
{
    public string CommandName => "subscriptions";

    public string SubCommandName => "remove";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var existingSub = await repo.GetGuildSubscription(context.GuildId, context.ChannelId);

        if (existingSub == null || !existingSub.Subscriptions.Any())
        {
            return Respond($"🤷‍♀️O RLY?", $"Did not find any subscription(s) in this channel to remove!");
        }

        EventSubscription eventSub = Enum.Parse<EventSubscription>(context.CommandInput!.Value);

        bool isLastSub = existingSub.Subscriptions.Count() == 1 && existingSub.Subscriptions.First() == eventSub;
        if (existingSub.LeagueId == null && (isLastSub || eventSub == EventSubscription.All))
        {
            await repo.DeleteGuildSubscription(context.GuildId, context.ChannelId);
            return Respond($"✅ Success!", $"Removed subscription to this channel.");
        }
        bool existingIsAll = existingSub.Subscriptions.Count() == 1 && existingSub.Subscriptions.First() == EventSubscription.All;
        if (existingIsAll && eventSub != EventSubscription.All)
        {
            var allTypes = EventSubscriptionHelper.GetAllSubscriptionTypes().ToList();
            allTypes.Remove(EventSubscription.All);
            await repo.UpdateGuildSubscription(existingSub with { Subscriptions = allTypes });
            var updatedFromAll = await repo.GetGuildSubscription(context.GuildId, context.ChannelId);
            return Respond($"✅ Success!", $"No longer subscribing to all events. Updated list:\n{Formatter.BulletPoints(updatedFromAll?.Subscriptions ?? Enumerable.Empty<EventSubscription>())}");
        }

        var updated = new List<EventSubscription>(existingSub.Subscriptions);

        if (eventSub == EventSubscription.All)
        {
            updated = new List<EventSubscription>();
        }
        else
        {
            updated.Remove(eventSub);
        }

        await repo.UpdateGuildSubscription(existingSub with { Subscriptions = updated });
        var regularUpdate = await repo.GetGuildSubscription(context.GuildId, context.ChannelId);
        if (regularUpdate?.Subscriptions.Any() == true)
        {
            return Respond($"✅ Success!", $"Unsubscribed from {eventSub}. Updated list:\n{Formatter.BulletPoints(regularUpdate.Subscriptions)}");
        }
        return Respond($"✅ Success!", $"No longer subscribing to any events.");

    }

    private static ChannelMessageWithSourceEmbedResponse Respond(string title, string content)
    {
        return new ChannelMessageWithSourceEmbedResponse()
        {
            Embeds = new List<RichEmbed>
            {
                new(title, content)
            }
        };
    }
}
