using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface ISlackTeamRepository
    {
        Task<SlackTeam> GetTeam(string teamId);
        Task DeleteByTeamId(string teamId);
        Task Insert(SlackTeam slackTeam);
        IAsyncEnumerable<SlackTeam> GetAllTeams();
        Task<IEnumerable<SlackTeam>> GetAllTeamsAsync();
    }
}