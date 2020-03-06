using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface ITransfersByGameWeek
    {
        Task<string> GetTransfersByGameweekTexts(int gw, int leagueId);
        Task<IEnumerable<TransfersByGameWeek.Transfer>> GetTransfersByGameweek(int gw, int leagueId);
    }
}