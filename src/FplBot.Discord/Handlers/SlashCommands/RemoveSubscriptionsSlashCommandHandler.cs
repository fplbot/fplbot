using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Discord.Data;

namespace FplBot.Discord.Handlers.SlashCommands
{
    public class RemoveSubscriptionSlashCommandHandler : ISlashCommandHandler
    {
        private readonly IGuildRepository _repo;

        public RemoveSubscriptionSlashCommandHandler(IGuildRepository repo)
        {
            _repo = repo;
        }

        public string CommandName => "subscriptions";

        public string SubCommandName => "remove";

        public async Task<SlashCommandResponse> Handle(SlashCommandContext slashCommandContext)
        {
            var existingSub = await _repo.GetGuildSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId);
            EventSubscription eventSub = Enum.Parse<EventSubscription>(slashCommandContext.CommandInput.Value);
            if (!existingSub.Subscriptions.Contains(eventSub))
            {
                return new ChannelMessageWithSourceResponse
                {
                    Content = $"You we're not subscribing to {slashCommandContext.CommandInput.Value} 🤷‍♂️"
                };
            }
            else
            {
                var existingSubsWithout = new List<EventSubscription>(existingSub.Subscriptions);
                existingSubsWithout.Remove(eventSub);

                await _repo.UpdateGuildSubscription(new GuildFplSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId, existingSubsWithout));
                var all = await _repo.GetGuildSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId);
                var content = $"Unsubbed. Updated list: {string.Join(",", all.Subscriptions)}";
                SlashCommandResponse channelMessageWithSourceResponse = new ChannelMessageWithSourceResponse() { Content = content };
                return channelMessageWithSourceResponse;
            }

        }
    }
}
