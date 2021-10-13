using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using Fpl.Client.Abstractions;
using FplBot.Discord.Data;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Discord.Handlers.SlashCommands
{
    public class HelpSlashCommandHandler : ISlashCommandHandler
    {
        private readonly IGuildRepository _store;
        private readonly ILeagueClient _client;
        private readonly IMessageSession _session;

        public HelpSlashCommandHandler(IGuildRepository store, ILeagueClient client, IMessageSession session)
        {
            _store = store;
            _client = client;
            _session = session;
        }
        public string CommandName => "help";

        public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
        {
            var content = "";
            var sub = await _store.GetGuildSubscription(context.GuildId, context.ChannelId);
            if (sub != null)
            {
                if (sub.LeagueId.HasValue)
                {
                    var league = await _client.GetClassicLeague(sub.LeagueId.Value, tolerate404:true);
                    if(league != null)
                        content += $"\n**League:**\nCurrently following the '{league.Properties.Name}' league";
                }
                else
                {
                    content += $"\n ⚠️ Not following any FPL leagues";
                }

                var allTypes = EventSubscriptionHelper.GetAllSubscriptionTypes();
                if (sub.Subscriptions.Any())
                {
                    content += $"\n\n**Subscriptions:**\n{string.Join("\n", sub.Subscriptions.Select(s => $" ✅ {s}"))}";

                    if (!sub.Subscriptions.Contains(EventSubscription.All))
                    {
                        var allTypesExceptSubs = allTypes.Except(sub.Subscriptions).Except(new []{ EventSubscription.All });
                        content += $"\n\n**Not subscribing:**\n{string.Join("\n", allTypesExceptSubs.Select(s => $" ❌ {s}"))}";
                    }
                }
                else
                {
                    content += "\n\n**Subscriptions:**\n ⚠️ No subscriptions";
                    content += $"\n\n**Events you may subscribe to:**\n{string.Join("\n", allTypes.Select(s => $" ▪️ {s}"))}";
                }
            }
            else
            {
                content = "⚠️ Not subscribing to any events. Add one to get notifications!";
            }

            return Respond(content);
        }

        private static ChannelMessageWithSourceEmbedResponse Respond(string content)
        {
            return new ChannelMessageWithSourceEmbedResponse() { Embeds = new List<RichEmbed>{ new RichEmbed("ℹ️ HELP", content)} };
        }
    }
}
