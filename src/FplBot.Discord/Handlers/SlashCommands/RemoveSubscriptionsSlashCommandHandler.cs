using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Discord.Data;
using FplBot.Formatting;

namespace FplBot.Discord.Handlers.SlashCommands;

public class RemoveSubscriptionSlashCommandHandler : ISlashCommandHandler
{
    private readonly IGuildRepository _repo;

    public RemoveSubscriptionSlashCommandHandler(IGuildRepository repo)
    {
        _repo = repo;
    }

    public string CommandName => "subscriptions";

    public string SubCommandName => "remove";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var existingSub = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);

        if (existingSub == null || !existingSub.Subscriptions.Any())
        {
            return Respond($"🤷‍♀️O RLY?", $"Did not find any subscription(s) in this channel to remove!");
        }

        EventSubscription eventSub = Enum.Parse<EventSubscription>(context.CommandInput.Value);

        bool isLastSub = existingSub.Subscriptions.Count() == 1 && existingSub.Subscriptions.First() == eventSub;
        if (existingSub.LeagueId == null && (isLastSub || eventSub == EventSubscription.All))
        {
            await _repo.DeleteGuildSubscription(context.GuildId, context.ChannelId);
            return Respond($"✅ Success!", $"Removed subscription to this channel.");
        }
        bool existingIsAll = existingSub.Subscriptions.Count() == 1 && existingSub.Subscriptions.First() == EventSubscription.All;
        if (existingIsAll && eventSub != EventSubscription.All)
        {
            var allTypes = EventSubscriptionHelper.GetAllSubscriptionTypes().ToList();
            allTypes.Remove(EventSubscription.All);
            await _repo.UpdateGuildSubscription(existingSub with { Subscriptions = allTypes });
            var updatedFromAll = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);
            return Respond($"✅ Success!", $"No longer subscribing to all events. Updated list:\n{Formatter.BulletPoints(updatedFromAll.Subscriptions)}");
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

        await _repo.UpdateGuildSubscription(existingSub with { Subscriptions = updated });
        var regularUpdate = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);
        if (regularUpdate.Subscriptions.Any())
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
