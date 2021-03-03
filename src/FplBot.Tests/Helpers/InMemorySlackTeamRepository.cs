using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Data;
using Fpl.Data.Repositories;
using FplBot.Core.Abstractions;

namespace FplBot.Tests.Helpers
{
    public class InMemorySlackTeamRepository : ISlackTeamRepository
    {
        private readonly long _leagueId;

        public InMemorySlackTeamRepository()
        {
            _leagueId = 15263;
        }

        public Task<SlackTeam> GetTeam(string teamId)
        {
            return Task.FromResult(new SlackTeam
            {
                FplbotLeagueId = _leagueId
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
                    FplbotLeagueId = _leagueId
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