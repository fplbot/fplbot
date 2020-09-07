using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplBotJoinedChannelHandler : IHandleEvent
    {
        private readonly ILogger<FplBotJoinedChannelHandler> _logger;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackClientService _slackClientService;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILeagueClient _leagueClient;

        public FplBotJoinedChannelHandler(ILogger<FplBotJoinedChannelHandler> logger,
            ISlackWorkSpacePublisher publisher,
            ISlackClientService slackClientService,
            ISlackTeamRepository teamRepo,
            ILeagueClient leagueClient
            )
        {
            _logger = logger;
            _publisher = publisher;
            _slackClientService = slackClientService;
            _teamRepo = teamRepo;
            _leagueClient = leagueClient;
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var joinedEvent = slackEvent as MemberJoinedChannelEvent;
            _logger.LogInformation(JsonConvert.SerializeObject(joinedEvent));
            var introMessage = ":wave: Hi, I'm fplbot. Type `@fplbot help` to see what I can do.";
            var team = await _teamRepo.GetTeam(eventMetadata.Team_Id);
            var league = await _leagueClient.GetClassicLeague((int)team.FplbotLeagueId);
            var setupMessage = $"I'm pushing notifications relevant to {league.Properties.Name} into {team.FplBotSlackChannel}";
            await _publisher.PublishToWorkspace(eventMetadata.Team_Id, joinedEvent.Channel, introMessage, setupMessage);
            return new EventHandledResponse("OK");
        }

        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is MemberJoinedChannelEvent;

        public (string HandlerTrigger, string Description) GetHelpDescription() => ("", "");
    }
}