using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models;

namespace FplBot.WebApi.Data
{
    public interface ISlackTeamRepository
    {
        Task DeleteByTeamId(string teamId);
        Task Insert(SlackTeam slackTeam);
        IAsyncEnumerable<SlackTeam> GetAllTeams();
    }
}