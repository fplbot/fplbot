using System.Collections.Generic;
using System.Threading.Tasks;

namespace FplBot.Formatting;

public interface ITransfersByGameWeek
{
    Task<string> GetTransfersByGameweekTexts(int gw, int leagueId, bool includeExternalLinks = true);
    Task<IEnumerable<TransfersByGameWeek.Transfer>> GetTransfersByGameweek(int gw, int leagueId);
    Task<TransfersByGameWeek.TransfersPayload> GetTransferMessages(int gw, int leagueId, bool includeExternalLinks = true);
}
