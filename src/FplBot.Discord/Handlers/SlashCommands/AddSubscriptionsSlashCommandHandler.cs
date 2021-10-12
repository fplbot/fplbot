using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Discord.Data;

namespace FplBot.Discord.Handlers.SlashCommands
{
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
                return Respond("✅ Success!", $"Added subscription to {string.Join(",", newSub.Subscriptions)}");
            }

            if (existingSub.Subscriptions.ContainsSubscriptionFor(newEventSub))
            {
                return Respond("⚠️", $"Already subscribing to {context.CommandInput.Value}");
            }

            var existingSubsWithNew = new List<EventSubscription>(existingSub.Subscriptions) { newEventSub };

            if (newEventSub == EventSubscription.All)
            {
                existingSubsWithNew = new List<EventSubscription> { EventSubscription.All };
            }

            await _repo.UpdateGuildSubscription(existingSub with { Subscriptions = existingSubsWithNew});
            var all = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);
            return Respond("ℹ️ Updated!", $"Now subscribing to {string.Join(",", all.Subscriptions)}");
        }

        private static ChannelMessageWithSourceEmbedResponse Respond(string title, string description)
        {
            return new ChannelMessageWithSourceEmbedResponse() { Embeds = new List<RichEmbed>{ new RichEmbed(title, description)}};
        }
    }
}
