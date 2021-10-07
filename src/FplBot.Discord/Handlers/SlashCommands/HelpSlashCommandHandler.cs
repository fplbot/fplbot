using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;

namespace FplBot.Discord.Handlers.SlashCommands
{
    public class HelpSlashCommandHandler : ISlashCommandHandler
    {
        public string CommandName => "help";

        public Task<SlashCommandResponse> Handle(SlashCommandContext slashCommandContext)
        {
            SlashCommandResponse channelMessageWithSourceResponse = new ChannelMessageWithSourceResponse { Content = "HELP!" };
            return Task.FromResult(channelMessageWithSourceResponse);
        }
    }
}
