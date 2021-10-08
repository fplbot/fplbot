using System.Collections.Generic;
using System.Threading.Tasks;

namespace FplBot.Formatting
{
    public interface ITransfersByGameWeek
    {
        Task<string> GetTransfersByGameweekTexts(int gw, int leagueId);
        Task<IEnumerable<TransfersByGameWeek.Transfer>> GetTransfersByGameweek(int gw, int leagueId);
    }
}
