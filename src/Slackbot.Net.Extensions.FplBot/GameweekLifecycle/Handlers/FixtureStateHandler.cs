using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class FixtureStateHandler
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILogger<FixtureStateHandler> _logger;

        public FixtureStateHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ILogger<FixtureStateHandler> logger)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
            _logger = logger;
        }

        public async Task OnFixturesProvisionalFinished(IEnumerable<FinishedFixture> provisionalFixturesFinished)
        {
            _logger.LogInformation("Handling fixture provisionally finished");

            var teams = await _slackTeamRepo.GetAllTeams();
            foreach (var slackTeam in teams)
            {
                var formatted = Formatter.FormatProvisionalFinished(provisionalFixturesFinished);
                await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted);
            }
        }
    }
}