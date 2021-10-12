using System.Linq;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Discord.Data;

namespace FplBot.Discord.Handlers.SlashCommands
{
    public class SubsSlashCommandHandler : ISlashCommandHandler
    {
        private readonly IGuildRepository _repo;

        public SubsSlashCommandHandler(IGuildRepository repo)
        {
            _repo = repo;
        }
        public string CommandName => "subs";

        public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
        {
            var sub = await _repo.GetGuildSubscription(context.GuildId, context.ChannelId);

            if (sub == null)
                return new ChannelMessageWithSourceResponse { Content = "No subscriptions registered in this channel. Add one?" };

            if (!sub.Subscriptions.Any())
                return new ChannelMessageWithSourceResponse { Content = "⚠️ No subscriptions in this channel" };

            return new ChannelMessageWithSourceResponse { Content = $"{string.Join("\n", sub.Subscriptions.Select(c => $"* {c}"))}" };
        }
    }
}
