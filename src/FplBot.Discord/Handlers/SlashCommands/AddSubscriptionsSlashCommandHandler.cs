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
            if (existingSub.Subscriptions.Contains(newEventSub))
            {
                return new ChannelMessageWithSourceResponse
                {
                    Content = $"Already subscribing to {slashCommandContext.CommandInput.Value}"
                };
            }
            else
            {
                var existingSubsWithNew = new List<EventSubscription>(existingSub.Subscriptions) { newEventSub };
                await _repo.UpdateGuildSubscription(new GuildFplSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId, existingSubsWithNew));
                var all = await _repo.GetGuildSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId);
                var content = $"Subscribing to {string.Join(",", all.Subscriptions)}";
                SlashCommandResponse channelMessageWithSourceResponse = new ChannelMessageWithSourceResponse() { Content = content };
                return channelMessageWithSourceResponse;
            }

        }
    }
}
