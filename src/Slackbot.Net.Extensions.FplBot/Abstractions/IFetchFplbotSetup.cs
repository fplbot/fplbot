using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IFetchFplbotSetup
    {
        Task<FplbotSetup> GetSetupByToken(string token);
        Task<IEnumerable<FplbotSetup>> GetAllFplBotSetup();
    }

    public class ConfigFplbotSetupFetcher : IFetchFplbotSetup
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
    }

    public class FplbotSetup
    {
        public int LeagueId { get; set; }
        public string Channel { get; set; }
    }
}