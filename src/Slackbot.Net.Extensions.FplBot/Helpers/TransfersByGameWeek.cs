using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Options;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class TransfersByGameWeek : ITransfersByGameWeek
    {
        private readonly FplbotOptions _fplbotOptions;
        private readonly ILeagueClient _leagueClient;
        private readonly IPlayerClient _playerClient;
        private readonly IGameweekHelper _gameweekHelper;
        private readonly ITransfersClient _transfersClient;
        private readonly IEntryClient _entryClient;

        public TransfersByGameWeek(IOptions<FplbotOptions> fplbotOptions, 
            ILeagueClient leagueClient,
            IPlayerClient playerClient,
            IGameweekHelper gameweekHelper,
            ITransfersClient transfersClient,
            IEntryClient entryClient
            )
        {
            _fplbotOptions = fplbotOptions.Value;
            _leagueClient = leagueClient;
            _playerClient = playerClient;
            _gameweekHelper = gameweekHelper;
            _transfersClient = transfersClient;
            _entryClient = entryClient;
        }
        
        public async Task<string> GetTransfersByGameweek(int? gw)
        {
            var leagueTask = _leagueClient.GetClassicLeague(_fplbotOptions.LeagueId);
            var playersTask = _playerClient.GetAllPlayers();

            var league = await leagueTask;
            var players = await playersTask;

            var sb = new StringBuilder();
            sb.Append($"Transfers made for gameweek {gw}:\n\n");

            await Task.WhenAll(league.Standings.Entries
                .OrderBy(x => x.Rank)
                .Select(entry => GetTransfersTextForEntry(entry, gw.Value, players))
                .ToArray()
                .ForEach(async task => sb.Append(await task)));

            return sb.ToString();
        }
        
        private async Task<string> GetTransfersTextForEntry(ClassicLeagueEntry entry, int gameweek, ICollection<Player> players)
        {
            var transfersTask = _transfersClient.GetTransfers(entry.Entry);
            var picksTask = _entryClient.GetPicks(entry.Entry, gameweek);

            var transfers = (await transfersTask).Where(x => x.Event == gameweek).Select(x => new
            {
                EntryId = x.Entry,
                PlayerTransferredOut = GetPlayerName(players, x.ElementOut),
                PlayerTransferredIn = GetPlayerName(players, x.ElementIn),
                SoldFor = x.ElementOutCost,
                BoughtFor = x.ElementInCost
            }).ToArray();

            var sb = new StringBuilder();

            sb.Append($"{entry.GetEntryLink(gameweek)} ");
            if (transfers.Any())
            {
                var picks = await picksTask;
                var transferCost = picks.EventEntryHistory.EventTransfersCost;
                var wildcardPlayed = picks.ActiveChip == Constants.ChipNames.Wildcard;
                var transferCostString = transferCost > 0 ? $" (-{transferCost} pts)" : wildcardPlayed ? " (:fire:wildcard:fire:)" : "";
                sb.Append($"transferred{transferCostString}:\n");
                foreach (var entryTransfer in transfers)
                {
                    sb.Append($"   {entryTransfer.PlayerTransferredOut} ({Formatter.FormatCurrency(entryTransfer.SoldFor)}) :arrow_right: {entryTransfer.PlayerTransferredIn} ({Formatter.FormatCurrency(entryTransfer.BoughtFor)})\n");
                }
            }
            else
            {
                sb.Append("made no transfers :shrug:\n");
            }

            return sb.ToString();
        }

        private static string GetPlayerName(IEnumerable<Player> players, int playerId)
        {
            var player = players.SingleOrDefault(x => x.Id == playerId);
            return player != null ? $"{player.FirstName} {player.SecondName}" : "";
        }
    }
}