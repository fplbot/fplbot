using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Discord.Handlers.SlashCommands
{
    public class HelpSlashCommandHandler : ISlashCommandHandler
    {
        private readonly IMessageSession _session;

        public HelpSlashCommandHandler(IMessageSession session)
        {
            _session = session;
        }
        public string CommandName => "help";

        public Task<SlashCommandResponse> Handle(SlashCommandContext slashCommandContext)
        {
            SlashCommandResponse channelMessageWithSourceResponse = new ChannelMessageWithSourceResponse { Content = "HELP!" };
            return Task.FromResult(channelMessageWithSourceResponse);
        }
    }
}
