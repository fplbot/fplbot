using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface ISlackTeamRepository
    {
        Task Insert(SlackTeam slackTeam);
        Task DeleteByTeamId(string teamId);
        IAsyncEnumerable<SlackTeam> GetAllTeams();
    }
}