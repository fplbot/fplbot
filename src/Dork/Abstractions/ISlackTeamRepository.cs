using System.Collections.Generic;
using System.Threading.Tasks;
using Dork.Models;

namespace Dork.Abstractions
{
    public interface ISlackTeamRepository
    {
        Task Insert(SlackTeam slackTeam);
        Task DeleteByTeamId(string teamId);
        IAsyncEnumerable<SlackTeam> GetAllTeams();
    }
}