using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Core.Handlers
{
    public class AppUninstaller : IUninstall
    {
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly IMessageSession _messageSession;

        public AppUninstaller(ISlackTeamRepository slackTeamRepo, IMessageSession messageSession)
        {
            _slackTeamRepo = slackTeamRepo;
            _messageSession = messageSession;
        }
        
        public async Task Uninstall(string teamId)
        {
            var team = await _slackTeamRepo.GetTeam(teamId);
            await _slackTeamRepo.DeleteByTeamId(teamId);
            await _messageSession.Publish(new AppUninstalled(team.TeamId, team.TeamName, (int)team.FplbotLeagueId, team.FplBotSlackChannel));
        }
    }
}