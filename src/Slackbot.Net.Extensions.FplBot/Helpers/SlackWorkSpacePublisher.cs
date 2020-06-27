using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    internal class SlackWorkSpacePublisher : ISlackWorkSpacePublisher
    {
        private ITokenStore _tokenStore;
        private readonly IFetchFplbotSetup _teamRepo;
        private readonly ISlackClientBuilder _slackClientBuilder;
        private readonly ILogger<SlackWorkSpacePublisher> _logger;

        public SlackWorkSpacePublisher(ITokenStore tokenStore, IFetchFplbotSetup teamRepo, ISlackClientBuilder builder, ILogger<SlackWorkSpacePublisher> logger)
        {
            _tokenStore = tokenStore;
            _teamRepo = teamRepo;
            _slackClientBuilder = builder;
            _logger = logger;
        }

        public async Task PublishToAllWorkspaces(string msg)
        {
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                await PublishUsingToken(token, msg);
            }
        }

        public async Task PublishUsingToken(string token, params string[] messages)
        {
            var setup = await _teamRepo.GetSetupByToken(token);
            var slackClient = _slackClientBuilder.Build(token);
            foreach (var message in messages)
            {
                try
                {
                    var res = await slackClient.ChatPostMessage(setup.Channel, message);

                    if (!res.Ok)
                    {
                        _logger.LogError($"Could not post to {setup.Channel}", res.Error);
                    }
                }
                catch (SlackApiException sae)
                {
                    if (sae.Message == "account_inactive")
                    {
                        await _tokenStore.Delete(token);
                        _logger.LogInformation($"Deleted inactive token");
                    }
                    else
                    {
                        _logger.LogError(sae, sae.Message);
                    }
                }
            }
        }

        public async Task PublishToSingleWorkspaceConnectedToLeague(int leagueId, params string[] messages)
        {
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var setup = await _teamRepo.GetSetupByToken(token);

                if (setup.LeagueId == leagueId)
                {
                    foreach (var msg in messages)
                    {
                        await PublishUsingToken(token, msg); 
                    }
                    
                }
            }
        }
    }
}