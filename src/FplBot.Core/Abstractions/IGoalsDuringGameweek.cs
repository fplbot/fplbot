using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface IGoalsDuringGameweek
    {
        Task<IDictionary<int, int>> GetPlayerGoals(int gameweek);
    }
}