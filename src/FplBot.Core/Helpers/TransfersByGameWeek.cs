using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.Helpers
{
    public class TransfersByGameWeek : ITransfersByGameWeek
    {
        private readonly ILeagueClient _leagueClient;
        private readonly IGlobalSettingsClient _globalSettingsClient;
        private readonly ITransfersClient _transfersClient;
        private readonly IEntryClient _entryClient;
        private readonly ILogger<TransfersByGameWeek> _logger;

        public TransfersByGameWeek(
            ILeagueClient leagueClient,
            IGlobalSettingsClient globalSettingsClient,
            ITransfersClient transfersClient,
            IEntryClient entryClient,
            ILogger<TransfersByGameWeek> logger)
        {
            _leagueClient = leagueClient;
            _globalSettingsClient = globalSettingsClient;
            _transfersClient = transfersClient;
            _entryClient = entryClient;
            _logger = logger;
        }

        public async Task<IEnumerable<Transfer>> GetTransfersByGameweek(int gw, int leagueId)
        {
            if (gw < 2)
            {
                return Enumerable.Empty<Transfer>();
            }

            try
            {
                var league = await _leagueClient.GetClassicLeague(leagueId);
            
                var playerTransfers = new ConcurrentBag<Transfer>();
                var entries = league.Standings.Entries;

                await Task.WhenAll(entries.Select(async entry =>
                {
                    var transfers = (await _transfersClient.GetTransfers(entry.Entry)).Where(x => x.Event == gw).Select(x =>
                    {
                        var e = entries.Single(e => e.Entry == x.Entry);
                        return new Transfer
                        {
                            EntryId = x.Entry,
                            EntryName = e.PlayerName,
                            EntryRealName = e.EntryName,
                            PlayerTransferredIn = x.ElementIn,
                            PlayerTransferredOut = x.ElementOut
                        };
                    });

                    foreach (var transfer in transfers)
                    {
                        playerTransfers.Add(transfer);
                    }
                }));

                return playerTransfers.ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);

                return Enumerable.Empty<Transfer>();
            }
        }

       

        public async Task<string> GetTransfersByGameweekTexts(int gw, int leagueId)
        {
            if (gw < 2)
            {
                return "No transfers are made the first gameweek.";
            }

            var leagueTask = _leagueClient.GetClassicLeague(leagueId);
            var settingsTask = _globalSettingsClient.GetGlobalSettings();

            var league = await leagueTask;
            var settings = await settingsTask;

            var sb = new StringBuilder();
            sb.Append($"Transfers made for gameweek {gw}:\n\n");

            var didNoTransfers = new ConcurrentBag<ClassicLeagueEntry>();

            await Task.WhenAll(league.Standings.Entries
                .OrderBy(x => x.Rank)
                .Select(entry => GetTransfersTextForEntry(entry, gw, settings.Players))
                .ToArray()
                .ForEach(async entryTransfersTask =>
                {
                    var entryTransfers = await entryTransfersTask;
                    if (!entryTransfers.DidTransfer)
                    {
                        didNoTransfers.Add(entryTransfers.Entry);
                    }
                    else
                    {
                        sb.Append(entryTransfers.Text);
                    }
                }));

            if (didNoTransfers.Count > 10)
            {
                sb.Append($"\nThe {didNoTransfers.Count} others saved their transfer :sleeping:");
            }
            else if (didNoTransfers.Count == 1)
            {
                sb.Append($"\n{didNoTransfers.Single().GetEntryLink(gw)} saved their transfer :sleeping:");
            }
            else if (didNoTransfers.Count > 0)
            {
                sb.Append($"\n{didNoTransfers.Select(x => x.GetEntryLink(gw)).Join()} saved their transfer :sleeping:");
            }

            return sb.ToString();
        }

        
        private async Task<EntryTranfers> GetTransfersTextForEntry(ClassicLeagueEntry entry, int gameweek, ICollection<Player> players)
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

            var hasTransfers = transfers.Any();
            var sb = new StringBuilder();

            if (hasTransfers)
            {
                try
                {
                    var picks = await picksTask;
                    var transferCost = picks.EventEntryHistory.EventTransfersCost;
                    var wildcardPlayed = picks.ActiveChip == FplConstants.ChipNames.Wildcard;
                    var freeHitPlayed = picks.ActiveChip == FplConstants.ChipNames.FreeHit;
                    if (wildcardPlayed)
                    {
                        sb.Append($"{entry.GetEntryLink(gameweek)} threw a WILDCAAAAARD :fire::fire::fire::\n");
                    } 
                    else if (freeHitPlayed)
                    {
                        sb.Append($"{entry.GetEntryLink(gameweek)} went all in with the Free Hit chip:\n");
                    }
                    else
                    {
                        var transferCostString = transferCost > 0 ? $" (-{transferCost} pts)" : "";
                        sb.Append($"{entry.GetEntryLink(gameweek)} transferred{transferCostString}:\n");
                    }
                    foreach (var entryTransfer in transfers)
                    {
                        sb.Append($"   :black_small_square:{entryTransfer.PlayerTransferredOut} ({Formatter.FormatCurrency(entryTransfer.SoldFor)}) :arrow_right: {entryTransfer.PlayerTransferredIn} ({Formatter.FormatCurrency(entryTransfer.BoughtFor)})\n");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                }
            }

            return new EntryTranfers
            {
                Entry = entry,
                DidTransfer = hasTransfers,
                Text = sb.ToString()
            };
        }

        private static string GetPlayerName(IEnumerable<Player> players, int playerId)
        {
            var player = players.SingleOrDefault(x => x.Id == playerId);
            return player != null ? $"{player.FirstName.First()}. {player.SecondName}" : "";
        }

        public class Transfer
        {
            public int EntryId { get; set; }
            public string EntryName { get; set; }
            public string EntryRealName { get; set; }
            public int PlayerTransferredOut { get; set; }
            public int PlayerTransferredIn { get; set; }
        }

        private class EntryTranfers
        {
            public ClassicLeagueEntry Entry { get; set; }
            public string Text { get; set; }
            public bool DidTransfer { get; set; }
        }
    }
}