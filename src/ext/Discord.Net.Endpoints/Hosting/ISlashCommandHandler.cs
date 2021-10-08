using System.Threading.Tasks;
using Discord.Net.Endpoints.Middleware;

namespace Discord.Net.Endpoints.Hosting
{
    public interface ISlashCommandHandler
    {
        public string CommandName { get; }

        public string SubCommandName => null;

        public Task<SlashCommandResponse> Handle(SlashCommandContext context);
    }
}
