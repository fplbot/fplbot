using System.Threading.Tasks;
using System.Collections.Generic;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface ITeamValue
    {
        Task<Dictionary<int, float>> GetTeamValuePerGameWeek(int teamCode);
        Task<Dictionary<int, float>> GetValueInBankPerGameWeek(int teamCode);
    }
}