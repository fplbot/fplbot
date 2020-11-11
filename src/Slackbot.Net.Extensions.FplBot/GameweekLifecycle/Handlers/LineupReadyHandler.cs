using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class LineupReadyHandler
    {
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILogger<LineupReadyHandler> _logger;

        public LineupReadyHandler(ISlackTeamRepository slackTeamRepo, ISlackWorkSpacePublisher publisher, ILogger<LineupReadyHandler> logger)
        {
            _slackTeamRepo = slackTeamRepo;
            _publisher = publisher;
            _logger = logger;
        }
        
        public async Task HandleLineupReady(Lineups lineups)
        {
            _logger.LogInformation("Handling new lineups");
            var slackTeam = await _slackTeamRepo.GetTeam("T0A9QSU83");
            if (slackTeam.FplBotSlackChannel == "#fpltest")
            {
                var formatted = Formatter.FormatLineup(lineups);
                await _publisher.PublishToWorkspace(slackTeam.TeamId, "#johntest", formatted);
            }
        }
    }
}