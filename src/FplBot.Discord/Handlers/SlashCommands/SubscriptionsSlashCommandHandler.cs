using System.Linq;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Discord.Data;

namespace FplBot.Discord.Handlers.SlashCommands
{
    public class SubscriptionsSlashCommandHandler : ISlashCommandHandler
    {
        private readonly IGuildRepository _repo;

        public SubscriptionsSlashCommandHandler(IGuildRepository repo)
        {
            _repo = repo;
        }
        public string CommandName => "subs";

        public async Task<SlashCommandResponse> Handle(SlashCommandContext slashCommandContext)
        {
            var subs = await _repo.GetAllSubscriptionInGuild(slashCommandContext.GuildId);
            string content = "Subscriptions: \n\n";

            if (!subs.Any())
            {
                content = "No subscriptions registered in any channel";
            }

            foreach (var sub in subs)
            {
                if (sub.ChannelId == slashCommandContext.ChannelId)
                {
                    content += $"This channel:\n{string.Join("\n", sub.Subscriptions.Select(c => $"* {c}"))}";
                }
                else
                {
                    content += "No subs in this channel";
                }
            }

            return new ChannelMessageWithSourceResponse { Content = content };

        }
    }
}
