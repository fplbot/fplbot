using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<SlashCommandResponse> Handle(SlashCommandContext slashCommandContext)
        {
            var existingSub = await _repo.GetGuildSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId);
            EventSubscription newEventSub = Enum.Parse<EventSubscription>(slashCommandContext.CommandInput.Value);

            if (existingSub == null)
            {
                await _repo.InsertGuildSubscription(new GuildFplSubscription(slashCommandContext.GuildId,
                    slashCommandContext.ChannelId, new[] { newEventSub }));
                var newSub = await _repo.GetGuildSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId);
                return new ChannelMessageWithSourceResponse() { Content = $"Now subscribing to {string.Join(",", newSub.Subscriptions)}" };
            }

            if (existingSub.Subscriptions.Contains(newEventSub))
            {
                return new ChannelMessageWithSourceResponse
                {
                    Content = $"Already subscribing to {slashCommandContext.CommandInput.Value}"
                };
            }



            var existingSubsWithNew = new List<EventSubscription>(existingSub.Subscriptions) { newEventSub };

            if (newEventSub == EventSubscription.All)
            {
                existingSubsWithNew = new List<EventSubscription> { EventSubscription.All };
            }

            await _repo.UpdateGuildSubscription(new GuildFplSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId, existingSubsWithNew));
            var all = await _repo.GetGuildSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId);
            return new ChannelMessageWithSourceResponse() { Content = $"Now subscribing to {string.Join(",", all.Subscriptions)}" };
        }
    }
}
