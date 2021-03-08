using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Data;
using Fpl.Data.Abstractions;
using Fpl.Data.Repositories;
using FplBot.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Core.Handlers
{
    public class FplBotJoinedChannelHandler : IHandleMemberJoinedChannel
    {
        private const string FplBotProdAppId = "AREFP62B1";
        private const string FplBotTestAppId = "ATDD4SFQ9";
        
        private readonly ILogger<FplBotJoinedChannelHandler> _logger;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackClientBuilder _slackClientService;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILeagueClient _leagueClient;

        public FplBotJoinedChannelHandler(ILogger<FplBotJoinedChannelHandler> logger,
            ISlackWorkSpacePublisher publisher,
            ISlackClientBuilder slackClientService,
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

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, MemberJoinedChannelEvent joinedEvent)
        {
            _logger.LogInformation(JsonConvert.SerializeObject(joinedEvent));
            _logger.LogInformation(JsonConvert.SerializeObject(eventMetadata));
            var team = await _teamRepo.GetTeam(eventMetadata.Team_Id);
            var slackClient = _slackClientService.Build(team.AccessToken);
            var userProfile = await slackClient.UserProfile(joinedEvent.User);
            if (userProfile.Profile.Api_App_Id == FplBotProdAppId || userProfile.Profile.Api_App_Id == FplBotTestAppId)
            {
                var introMessage = ":wave: Hi, I'm fplbot. Type `@fplbot help` to see what I can do.";
                var league = await _leagueClient.GetClassicLeague((int)team.FplbotLeagueId);
                var setupMessage = $"I'm pushing notifications relevant to {league.Properties.Name} into {team.FplBotSlackChannel}";
                await _publisher.PublishToWorkspace(eventMetadata.Team_Id, joinedEvent.Channel, introMessage, setupMessage);
                return new EventHandledResponse("OK"); 
            }
            return new EventHandledResponse($"IGNORED FOR {userProfile.Profile.Real_Name}");
        }
    }
}