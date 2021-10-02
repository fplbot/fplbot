using System.Threading.Tasks;
using Discord.Net.Endpoints.Middleware;

namespace Discord.Net.Endpoints.Hosting
{
    public interface ISlashCommandHandler
    {
        public string Name { get; }
        public Task<SlashCommandResponse> Handle(SlashCommandContext slashCommandContext);
    }
}
