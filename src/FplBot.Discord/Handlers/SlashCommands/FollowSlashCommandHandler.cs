using System.Linq;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using Fpl.Client.Abstractions;
using FplBot.Discord.Data;

namespace FplBot.Discord.Handlers.SlashCommands
{
    public class FollowSlashCommandHandler : ISlashCommandHandler
    {
        private readonly ILeagueClient _leagueClient;
        private readonly IGuildRepository _repo;

        public FollowSlashCommandHandler(ILeagueClient leagueClient, IGuildRepository repo)
        {
            _leagueClient = leagueClient;
            _repo = repo;
        }

        public string CommandName => "follow";

        public async Task<SlashCommandResponse> Handle(SlashCommandContext slashCommandContext)
        {
            var leagueId = int.Parse(slashCommandContext.CommandInput.Value);
            var league = await _leagueClient.GetClassicLeague(leagueId, tolerate404:true);

            if(league == null)
                return new ChannelMessageWithSourceResponse { Content = $"Could not find a classic league of id '{leagueId}'" };


            var content = $"✅ Thx! Now following the '{$"{league.Properties.Name}"}' FPL league. ";

            var existingSub = await _repo.GetGuildSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId);
            if (existingSub == null)
            {
                await _repo.InsertGuildSubscription(new GuildFplSubscription(slashCommandContext.GuildId, slashCommandContext.ChannelId, leagueId, new []
                {
                    EventSubscription.All
                }));
                content += "\n\nNo existing subs, so also auto-subscribed to all FPL events (goals, standings, etc), but feel free to modify what events you would like to have using the subscription slash command";
            }

            return new ChannelMessageWithSourceResponse { Content = content };
        }
    }
}
