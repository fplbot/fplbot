using System.Collections.Generic;
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
            // await _session.Publish(new GameweekJustBegan(new NewGameweek(6)));
            await _session.Publish(new GameweekFinished(new FinishedGameweek(6)));
            var content = "";
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

            return new ChannelMessageWithSourceEmbedResponse() { Embeds = new List<RichEmbed>{ new RichEmbed("ℹ️ HELP", content)} };
        }
    }
}
