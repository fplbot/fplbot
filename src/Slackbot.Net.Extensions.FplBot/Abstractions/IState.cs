using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    public interface IState
    {
        Task Reset(int newGameweek);
        
        IEnumerable<SlackTeam> GetActiveTeams();
        Task Refresh(int gameweek);
        GameweekLeagueContext GetGameweekLeagueContext(string teamId);
        event Func<IEnumerable<FixtureEvents>, Task> OnNewFixtureEvents;
    }
}    