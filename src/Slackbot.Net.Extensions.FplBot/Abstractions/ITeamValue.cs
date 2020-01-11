using System.Threading.Tasks;
using System.Collections.Generic;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface ITeamValue
    {
        Task<Dictionary<int, double>> GetTeamValuePerGameWeek(int teamCode);
        Task<Dictionary<int, double>> GetValueInBankPerGameWeek(int teamCode);
    }
}