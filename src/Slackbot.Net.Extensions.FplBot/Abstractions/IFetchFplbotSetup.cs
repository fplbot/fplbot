using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IFetchFplbotSetup
    {
        Task<FplbotSetup> GetSetupByToken(string token);
        Task<IEnumerable<FplbotSetup>> GetAllFplBotSetup();
    }

    public class ConfigFplbotSetupFetcher : IFetchFplbotSetup, ISlackTeamRepository
    {
        private readonly IOptions<FplbotOptions> _options;

        public ConfigFplbotSetupFetcher(IOptions<FplbotOptions> options)
        {
            _options = options;
        }

        public Task<FplbotSetup> GetSetupByToken(string token)
        {
            return Task.FromResult(new FplbotSetup
            {
                LeagueId = _options.Value.LeagueId,
                Channel = _options.Value.Channel
            });        
        }

        public Task<IEnumerable<FplbotSetup>> GetAllFplBotSetup()
        {
            var fplbotSetups = new List<FplbotSetup>()
            {
                new FplbotSetup
                {
                    LeagueId = _options.Value.LeagueId,
                    Channel = _options.Value.Channel
                }
            };
            return Task.FromResult(fplbotSetups.AsEnumerable());
        }

        public Task<SlackTeam> GetTeam(string teamId)
        {
            return Task.FromResult(new SlackTeam
            {
                FplbotLeagueId = _options.Value.LeagueId
            });
        }

        public Task DeleteByTeamId(string teamId)
        {
            throw new System.NotImplementedException();
        }

        public Task Insert(SlackTeam slackTeam)
        {
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<SlackTeam> GetAllTeams()
        {
            yield return new SlackTeam
            {
                FplbotLeagueId = _options.Value.LeagueId
            };
        }

        public Task<IEnumerable<SlackTeam>> GetAllTeamsAsync()
        {
            IEnumerable<SlackTeam> teams = new []{ 
                new SlackTeam
                {
                    FplbotLeagueId = _options.Value.LeagueId
                } 
            };
            return Task.FromResult(teams);
        }
    }

    public class FplbotSetup
    {
        public int LeagueId { get; set; }
        public string Channel { get; set; }
    }
}