using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    internal class SlackWorkSpacePublisher : ISlackWorkSpacePublisher
    {
        private readonly ITokenStore _tokenStore;
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

        public async Task PublishTAllWorkspaceChannels(string msg)
        {
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                await PublishToWorkspaceChannelUsingToken(token, msg);
            }
        }

        public async Task PublishToWorkspaceChannelUsingToken(string token, params string[] messages)
        {
            var setup = await _teamRepo.GetSetupByToken(token);
            await PublishToChannel(token, setup.Channel, messages);
        }

        public async Task PublishToWorkspaceChannelConnectedToLeague(int leagueId, params string[] messages)
        {
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var setup = await _teamRepo.GetSetupByToken(token);

                if (setup.LeagueId == leagueId)
                {
                    foreach (var msg in messages)
                    {
                        await PublishToWorkspaceChannelUsingToken(token, msg); 
                    }
                }
            }
        }

        public async Task PublishToWorkspace(string teamId, string channel, params string[] messages)
        {
            var token = await _tokenStore.GetTokenByTeamId(teamId);
            foreach (var msg in messages)
            {
                await PublishToChannel(token,channel, msg);
            }
        }

        private async Task PublishToChannel(string token, string channel, params string[] messages)
        {
            var slackClient = _slackClientBuilder.Build(token);
            foreach (var message in messages)
            {
                try
                {
                    var res = await slackClient.ChatPostMessage(new ChatPostMessageRequest
                    {
                        Channel = channel,
                        Text = message,
                        unfurl_links = "false"
                    });

                    if (!res.Ok)
                    {
                        _logger.LogError($"Could not post to {channel}", res.Error);
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
                catch (Exception e)
                {
                    _logger.LogInformation(e, e.Message);
                }
            }
        }
    }
}