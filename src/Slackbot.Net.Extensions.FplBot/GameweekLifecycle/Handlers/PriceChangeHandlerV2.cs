using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class PriceChangeHandlerV2
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _slackTeamRepo;

        public PriceChangeHandlerV2(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo)
        {
            _publisher = publisher;
            _slackTeamRepo = slackTeamRepo;
        }

        public async Task OnPriceChanges(IEnumerable<PriceChange> priceChanges)
        {
            var slackTeams = await _slackTeamRepo.GetAllTeams();
            foreach (var slackTeam in slackTeams)
            {
                // beta-testing
                if (slackTeam.TeamId == "T0A9QSU83")
                {
                    var formatted = Formatter.FormatPriceChanged(priceChanges);
                    await _publisher.PublishToWorkspace(slackTeam.TeamId, "#johntest", formatted);
                }
            }
        }
    }
}