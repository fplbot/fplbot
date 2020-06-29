using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class AppUninstalledHandler : IHandleEvent
    {
        private readonly ISlackTeamRepository _slackTeamRepo;

        public AppUninstalledHandler(ISlackTeamRepository slackTeamRepo)
        {
            _slackTeamRepo = slackTeamRepo;
        }
        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            await _slackTeamRepo.DeleteByTeamId(eventMetadata.Team_Id);
            return new EventHandledResponse($"Team {eventMetadata.Team_Id} uninstalled");
        }

        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppUninstalledEvent;
        public (string HandlerTrigger, string Description) GetHelpDescription() => ("?", "");
    }
}