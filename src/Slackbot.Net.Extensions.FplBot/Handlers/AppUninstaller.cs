using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class AppUninstaller : IUninstall
    {
        private readonly ISlackTeamRepository _slackTeamRepo;

        public AppUninstaller(ISlackTeamRepository slackTeamRepo)
        {
            _slackTeamRepo = slackTeamRepo;
        }
        
        public async Task Uninstall(string teamId)
        {
            await _slackTeamRepo.DeleteByTeamId(teamId);
        }
    }
}