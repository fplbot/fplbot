using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Core;
using FplBot.Core.Abstractions;
using Microsoft.Extensions.Options;

namespace FplBot.Tests.Helpers
{
    public class InMemorySlackTeamRepository : ISlackTeamRepository
    {
        private readonly IOptions<FplbotOptions> _options;

        public InMemorySlackTeamRepository(IOptions<FplbotOptions> options)
        {
            _options = options;
        }

        public Task<SlackTeam> GetTeam(string teamId)
        {
            return Task.FromResult(new SlackTeam
            {
                FplbotLeagueId = _options.Value.LeagueId
            });
        }

        public Task UpdateLeagueId(string teamId, long newLeagueId)
        {
            return Task.CompletedTask;
        }

        public Task DeleteByTeamId(string teamId)
        {
            throw new System.NotImplementedException();
        }

        public Task Insert(SlackTeam slackTeam)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<SlackTeam>> GetAllTeams()
        {
            IEnumerable<SlackTeam> teams = new []{ 
                new SlackTeam
                {
                    FplbotLeagueId = _options.Value.LeagueId
                } 
            };
            return Task.FromResult(teams);
        }

        public Task UpdateChannel(string teamId, string newChannel)
        {
            return Task.CompletedTask;
        }

        public Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions)
        {
            return Task.CompletedTask;
        }
    }
}