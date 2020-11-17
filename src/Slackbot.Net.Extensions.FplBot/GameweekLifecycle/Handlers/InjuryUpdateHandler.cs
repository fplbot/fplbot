using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class InjuryUpdateHandler
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILogger<InjuryUpdateHandler> _logger;

        public InjuryUpdateHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ILogger<InjuryUpdateHandler> logger)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
            _logger = logger;
        }
        
        public async Task OnInjuryUpdates(IEnumerable<PlayerUpdate> injuryUpdates)
        {
            _logger.LogInformation($"Handling player {injuryUpdates.Count()} status updates");
            var slackTeam = await _slackTeamRepo.GetTeam("T0A9QSU83");
            if (slackTeam.FplBotSlackChannel == "#fpltest")
            {
                var formatted = Formatter.FormatStatusUpdates(injuryUpdates);
                await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted);    
            }
            else
            {
                var filtered = injuryUpdates.Where(c => c.ToPlayer.IsRelevant());
                if (filtered.Any())
                {
                    var formatted = Formatter.FormatStatusUpdates(filtered);
                    await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted);    
                }
                else
                {
                    _logger.LogInformation("All updates filtered out because of irrelevant, so not sending any notification");
                }
            }
        }
    }
}