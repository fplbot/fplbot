using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
{
    internal interface ITransfersByGameWeek
    {
        Task<string> GetTransfersByGameweek(int? gw);
    }
}