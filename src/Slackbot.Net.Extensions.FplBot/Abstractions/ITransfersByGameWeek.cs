using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface ITransfersByGameWeek
    {
        Task<string> GetTransfersByGameweekTexts(int? gw);
        Task<IEnumerable<TransfersByGameWeek.Transfer>> GetTransfersByGameweek(int gw);
    }
}