using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Data.Models;

namespace Fpl.Data.Abstractions
{
    public interface ISlackTeamRepository
    {
        Task<SlackTeam> GetTeam(string teamId);
        Task UpdateLeagueId(string teamId, long newLeagueId);
        Task DeleteByTeamId(string teamId);
        Task Insert(SlackTeam slackTeam);
        Task<IEnumerable<SlackTeam>> GetAllTeams();
        Task UpdateChannel(string teamId, string newChannel);
        Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions);
    }
}
