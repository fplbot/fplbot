using System;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Core.Helpers
{
    internal class SlackWorkSpacePublisher : ISlackWorkSpacePublisher
    {
        private readonly ITokenStore _tokenStore;
        private readonly ISlackTeamRepository _repository;
        private readonly ISlackClientBuilder _slackClientBuilder;
        private readonly ILogger<SlackWorkSpacePublisher> _logger;

        public SlackWorkSpacePublisher(ITokenStore tokenStore, ISlackTeamRepository repository, ISlackClientBuilder builder, ILogger<SlackWorkSpacePublisher> logger)
        {
            _tokenStore = tokenStore;
            _repository = repository;
            _slackClientBuilder = builder;
            _logger = logger;
        }

        public async Task PublishToAllWorkspaceChannels(string msg)
        {
            var teams = await _repository.GetAllTeams();
            foreach (var team in teams)
            {
                await PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, msg);
            }
        }

        public async Task PublishToWorkspace(string teamId, string channel, params string[] messages)
        {
            foreach (var msg in messages)
            {
                var req = new ChatPostMessageRequest
                {
                    Channel = channel,
                    Text = msg,
                    unfurl_links = "false"
                };
                await PublishToWorkspace(teamId, req);
            }
        }

        public async Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] messages)
        {
            var token = await _tokenStore.GetTokenByTeamId(teamId);
            await PublishUsingToken(token,messages);
        }

        private async Task PublishUsingToken(string token, params ChatPostMessageRequest[] messages)
        {
            var slackClient = _slackClientBuilder.Build(token);
            foreach (var message in messages)
            {
                try
                {
                    var res = await slackClient.ChatPostMessage(message);

                    if (!res.Ok)
                    {
                        _logger.LogWarning($"Could not post to {message.Channel}. {res.Error}");
                    }
                }
                catch (WellKnownSlackApiException sae)
                {
                    if (sae.Error == "account_inactive")
                    {
                        await _tokenStore.Delete(token);
                        _logger.LogInformation($"Deleted inactive token");
                    }
                    else
                    {
                        _logger.LogWarning(sae, $"Could not post to {message.Channel}. {sae.Error} {sae.ResponseContent}") ;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, e.Message);
                }
            }
        }
    }
}
