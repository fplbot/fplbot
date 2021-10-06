using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;

namespace FplBot.Discord.Handlers.SlashCommands
{
    public class SubscribeSlashCommandHandler : ISlashCommandHandler
    {
        public string Name => "subscribe";
        public Task<SlashCommandResponse> Handle(SlashCommandContext slashCommandContext)
        {
            SlashCommandResponse channelMessageWithSourceResponse = new ChannelMessageWithSourceResponse() { Content = slashCommandContext.CommandInput.Value };
            return Task.FromResult(channelMessageWithSourceResponse);
        }
    }
}
