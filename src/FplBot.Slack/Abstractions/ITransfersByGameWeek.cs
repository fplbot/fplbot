using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Slack.Helpers;

namespace FplBot.Slack.Abstractions
{
    public interface ITransfersByGameWeek
    {
        Task<string> GetTransfersByGameweekTexts(int gw, int leagueId);
        Task<IEnumerable<TransfersByGameWeek.Transfer>> GetTransfersByGameweek(int gw, int leagueId);
    }
}
