using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Data.Discord;
using FplBot.Discord.Data;
using FplBot.Formatting;

namespace FplBot.Discord.Handlers.SlashCommands;

public class AddSubscriptionSlashCommandHandler : ISlashCommandHandler
{
    private readonly IGuildRepository _repo;

    public AddSubscriptionSlashCommandHandler(IGuildRepository repo)
    {
        _repo = repo;
    }

    public string CommandName => "subscriptions";

    public string SubCommandName => "add";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var existingSub = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);
        EventSubscription newEventSub = Enum.Parse<EventSubscription>(context.CommandInput.Value);

        if (existingSub == null)
        {
            await _repo.InsertGuildSubscription(new GuildFplSubscription(context.GuildId, context.ChannelId, null, new[] { newEventSub }));
            var newSub = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);
            return Respond("✅ Success!", $"Added new subscription! Subscriptions:\n{Formatter.BulletPoints(newSub.Subscriptions)}");
        }

        if (existingSub.Subscriptions.Contains(newEventSub))
        {
            return Respond("⚠️", $"Already subscribing to {context.CommandInput.Value}");
        }

        var updatedList = new List<EventSubscription>(existingSub.Subscriptions) { newEventSub };

        if (newEventSub == EventSubscription.All)
        {
            updatedList = new List<EventSubscription> { EventSubscription.All };
        }
        else if (existingSub.Subscriptions.Count() == 1 && existingSub.Subscriptions.First() == EventSubscription.All) // from "all" to "1 specific" => 1 specifc
        {
            updatedList = new List<EventSubscription> { newEventSub };
        }

        await _repo.UpdateGuildSubscription(existingSub with { Subscriptions = updatedList});
        var all = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);
        return Respond("✅ Success!", $"Updated subscriptions:\n{Formatter.BulletPoints(all.Subscriptions)}");
    }

    private static ChannelMessageWithSourceEmbedResponse Respond(string title, string description)
    {
        return new ChannelMessageWithSourceEmbedResponse() { Embeds = new List<RichEmbed>{ new RichEmbed(title, description)}};
    }
}
