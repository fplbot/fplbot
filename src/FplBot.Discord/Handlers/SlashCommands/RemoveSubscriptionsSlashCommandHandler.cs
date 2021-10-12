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

        public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
        {
            var existingSub = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);

            if (existingSub == null)
            {
                return Respond($"⚠️ Did not find any sub in this channel to remove!");
            }

            EventSubscription eventSub = Enum.Parse<EventSubscription>(context.CommandInput.Value);

            bool existingIsAll = existingSub.Subscriptions.Count() == 1 && existingSub.Subscriptions.First() == EventSubscription.All;
            bool hasAllAndUnsubAll = existingIsAll && eventSub == EventSubscription.All;

            if (hasAllAndUnsubAll)
            {
                await _repo.DeleteGuildSubscription(context.GuildId, context.ChannelId);
                return Respond($"Unsubbed all events in this channel.");
            }

            if (existingIsAll && eventSub != EventSubscription.All)
            {
                var allTypes = EventSubscriptionHelper.GetAllSubscriptionTypes().ToList();
                allTypes.Remove(EventSubscription.All);
                await _repo.UpdateGuildSubscription(existingSub with { Subscriptions = allTypes });
                var updatedFromAll = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);
                return Respond($"No longer subscribing to all events. Updated list: {string.Join(",", updatedFromAll.Subscriptions)}");
            }

            var existingSubsWithout = new List<EventSubscription>(existingSub.Subscriptions);
            existingSubsWithout.Remove(eventSub);

            await _repo.UpdateGuildSubscription(existingSub with { Subscriptions = existingSubsWithout });
            var regularUpdate = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);
            return Respond($"Unsubscribed from {eventSub}. Updated list: {string.Join(",", regularUpdate.Subscriptions)}");
        }

        private static ChannelMessageWithSourceEmbedResponse Respond(string content)
        {
            return new ChannelMessageWithSourceEmbedResponse()
            {
                Embeds = new List<RichEmbed>
                {
                    new($"ℹ️", content)
                }
            };
        }
    }
}
