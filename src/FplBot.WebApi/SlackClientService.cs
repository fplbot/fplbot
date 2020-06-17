using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Dynamic;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi
{
    internal class SlackClientService : ISlackClientService
    {
        private readonly ITokenStore _tokenStore;
        private readonly ISlackClientBuilder _clientFactory;

        public SlackClientService(ITokenStore tokenStore, ISlackTeamRepository teamsClient,  ISlackClientBuilder clientFactory)
        {
            _tokenStore = tokenStore;
            _clientFactory = clientFactory;
        }

        public async Task<ISlackClient> CreateClient(string teamId)
        {
            var tokenForTeam = await _tokenStore.GetTokenByTeamId(teamId);
            return _clientFactory.Build(tokenForTeam);
        }
    }
}