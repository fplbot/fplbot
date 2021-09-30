using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Abstractions;
using FplBot.Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Core.Handlers
{
    public class FplBotJoinedChannelHandler : IHandleMemberJoinedChannel
    {
        private readonly ILogger<FplBotJoinedChannelHandler> _logger;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackClientBuilder _slackClientService;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILeagueClient _leagueClient;
        private readonly string _slackAppId;

        public FplBotJoinedChannelHandler(ILogger<FplBotJoinedChannelHandler> logger,
            ISlackWorkSpacePublisher publisher,
            ISlackClientBuilder slackClientService,
            ISlackTeamRepository teamRepo,
            ILeagueClient leagueClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _publisher = publisher;
            _slackClientService = slackClientService;
            _teamRepo = teamRepo;
            _leagueClient = leagueClient;
            _slackAppId = configuration["SlackAppId"];
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, MemberJoinedChannelEvent joinedEvent)
        {
            var team = await _teamRepo.GetTeam(eventMetadata.Team_Id);
            var slackClient = _slackClientService.Build(team.AccessToken);
            var userProfile = await slackClient.UserProfile(joinedEvent.User);
            if (userProfile.Profile.Api_App_Id == _slackAppId)
            {
                var introMessage = ":wave: Hi, I'm fplbot. Type `@fplbot help` to see what I can do.";
                var setupMessage = "";
                if (team.FplbotLeagueId.HasValue)
                {
                    try
                    {
                        var league = await _leagueClient.GetClassicLeague(team.FplbotLeagueId.Value);
                        if (!string.IsNullOrEmpty(team.FplBotSlackChannel))
                        {
                            setupMessage = $"I'm pushing notifications relevant to {league.Properties.Name} into {ChannelName()}. ";
                            if (team.FplBotSlackChannel != joinedEvent.Channel)
                            {
                                setupMessage += "If you want to have notifications in this channel instead, use the `@fplbot follow` command in this channel.";
                            }

                            // Back-compat as we currently have a mix of:
                            // - display names (#name)
                            // - channel_ids (C12351)
                            // Man be removed next season when we require updates to leagueids
                            string ChannelName()
                            {
                                return team.FplBotSlackChannel.StartsWith("#") ? team.FplBotSlackChannel : $"<#{team.FplBotSlackChannel}>";
                            }
                        }
                    }
                    catch (HttpRequestException e) when (e.Message.Contains("404"))
                    {
                        setupMessage = $"I'm currently following no valid league. The invalid leagueid is `{team.FplbotLeagueId}`. Use `@fplbot follow` to setup a new valid leagueid.";
                    }
                }
                else
                {
                    setupMessage = "To get notifications for a league, use my `@fplbot follow` command";
                }

                await _publisher.PublishToWorkspace(eventMetadata.Team_Id, joinedEvent.Channel, introMessage, setupMessage);
                return new EventHandledResponse("OK");
            }
            return new EventHandledResponse($"IGNORED FOR {userProfile.Profile.Real_Name}");
        }
    }
}
