using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class StatusUpdateHandler
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;


        public StatusUpdateHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
        }
        
        public async Task OnStatusUpdates(IEnumerable<PlayerStatusUpdate> statusUpdates)
        {
            var slackTeams = await _slackTeamRepo.GetAllTeams();
            foreach (var slackTeam in slackTeams)
            {
                if (slackTeam.TeamId == "T0A9QSU83")
                {
                    var formatted = Formatter.FormatStatusUpdates(statusUpdates);
                    await _publisher.PublishToWorkspace(slackTeam.TeamId, "#johntest", formatted);
                }
            }        
        }
    }
}