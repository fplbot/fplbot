using System.Collections.Generic;
using System.Threading.Tasks;

namespace FplBot.Core.Abstractions
{
    internal interface IGoalsDuringGameweek
    {
        Task<IDictionary<int, int>> GetPlayerGoals(int gameweek);
    }
}