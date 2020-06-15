using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.WebApi.Data;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Dynamic;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi
{
    internal class SlackClientService : ISlackClientService
    {
        private readonly ITokenStore _tokenStore;
        private readonly ISlackTeamRepository _teamsClient;
        private readonly ISlackClientBuilder _clientFactory;
        private readonly ILogger<SlackClientService> _logger;

        public SlackClientService(ITokenStore tokenStore, ISlackTeamRepository teamsClient,  ISlackClientBuilder clientFactory, ILogger<SlackClientService> logger)
        {
            _tokenStore = tokenStore;
            _teamsClient = teamsClient;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<ISlackClient> CreateClient(string teamId)
        {
            var tokenForTeam = await _tokenStore.GetTokenByTeamId(teamId);
            await foreach (var t in _teamsClient.GetAllTeams())
            {
                _logger.LogInformation($"'{t.TeamId}' - '{t.TeamName}'");
            }

            return _clientFactory.Build(tokenForTeam);
        }
    }
}