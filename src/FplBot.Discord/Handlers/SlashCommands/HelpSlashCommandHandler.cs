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

        public HelpSlashCommandHandler(IGuildRepository store, ILeagueClient client)
        {
            _store = store;
            _client = client;
        }
        public string CommandName => "help";

        public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
        {
            var content = "HELP!";
            var sub = await _store.GetGuildSubscription(context.GuildId, context.ChannelId);
            if (sub != null)
            {
                if (sub.LeagueId.HasValue)
                {
                    var league = await _client.GetClassicLeague(sub.LeagueId.Value, tolerate404:true);
                    if(league != null)
                        content += $"\nCurrently following the '{league.Properties.Name}' league";
                }
                else
                {
                    content += $"\nNot following any FPL leagues";
                }

                content += $"\nSubscriptions: {string.Join(",", sub.Subscriptions)}";
            }

            return new ChannelMessageWithSourceResponse { Content = content };
        }
    }
}
