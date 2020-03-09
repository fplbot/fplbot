using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace FplBot.WebApi.Data
{
    public interface ISlackTeamRepository
    {
        Task Insert(SlackTeam slackTeam);
        Task DeleteByTeamId(string teamId);
        Task<IEnumerable<FplbotSetup>> GetAllWorkspaces();
        IAsyncEnumerable<SlackTeam> GetAllTeams();
    }
}