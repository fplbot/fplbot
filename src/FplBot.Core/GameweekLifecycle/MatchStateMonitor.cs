using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    public class MatchStateMonitor : IMatchStateMonitor
    {

        private readonly ILogger<MatchStateMonitor> _logger;
        private readonly MatchState _matchState;

        public MatchStateMonitor(MatchState matchState, LineupReadyHandler lineupReadyHandler, ILogger<MatchStateMonitor> logger)
        {
            _logger = logger;
            _matchState = matchState;
            _matchState.OnLineUpReady += lineupReadyHandler.HandleLineupReady;
        }
        
        public async Task Initialize(int gw)
        {
            _logger.LogInformation("Init");
            await _matchState.Reset(gw);
        }

        public async Task HandleGameweekStarted(int gw)
        {
            _logger.LogInformation("Resetting state");
            await _matchState.Reset(gw);
        }
        public async Task HandleGameweekOngoing(int gw)
        {
            _logger.LogInformation("Refreshing state for ongoing gw");
            await _matchState.Refresh(gw);
        }

        public async Task HandleGameweekCurrentlyFinished(int gw)
        {
            _logger.LogInformation("Refreshing state for finished gw");
            await _matchState.Refresh(gw+1); // monitor next gameweeks matches, since current = finished 
        }
    }
}