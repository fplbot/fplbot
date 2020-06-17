using System.Threading.Tasks;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Dynamic;
using Slackbot.Net.SlackClients.Http;

namespace Slackbot.Net.Endpoints
{
    internal class SlackClientService : ISlackClientService
    {
        private readonly ITokenStore _tokenStore;
        private readonly ISlackClientBuilder _clientFactory;

        public SlackClientService(ITokenStore tokenStore, ISlackClientBuilder clientFactory)
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