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
    
    public class SlackTeam
    {
        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public string Scope { get; set; }
        public string AccessToken { get; set; }
        public string FplBotSlackChannel { get; set; }
        public long FplbotLeagueId { get; set; }
    }
}