using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class StatusUpdateHandler
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILogger<StatusUpdateHandler> _logger;

        public StatusUpdateHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ILogger<StatusUpdateHandler> logger)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
            _logger = logger;
        }
        
        public async Task OnStatusUpdates(IEnumerable<PlayerStatusUpdate> statusUpdates)
        {
            _logger.LogInformation("Handling player status updates");
            var slackTeam = await _slackTeamRepo.GetTeam("T0A9QSU83");
            if (slackTeam.FplBotSlackChannel == "#fpltest")
            {
                var formatted = Formatter.FormatStatusUpdates(statusUpdates);
                await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted);    
            }
            else
            {
                var filtered = statusUpdates.Where(IsRelevant);
                var formatted = Formatter.FormatStatusUpdates(filtered);
                await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted);    
            }
        }

        private bool IsRelevant(PlayerStatusUpdate update)
        {
            if (update.ToPlayer?.OwnershipPercentage > 2)
            {
                return true;
            }

            return update.ToPlayer?.NowCost > 55;
        }

        public async Task OnFixturesProvisionalFinished(IEnumerable<FinishedFixture> provisionalFixturesFinished)
        {
            _logger.LogInformation("Handling fixture provisionally finished");

            var slackTeam = await _slackTeamRepo.GetTeam("T0A9QSU83");
            if (slackTeam.FplBotSlackChannel == "#fpltest")
            {
                var formatted = Formatter.FormatProvisionalFinished(provisionalFixturesFinished);
                await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted.ToArray());
            }
        }
    }
}