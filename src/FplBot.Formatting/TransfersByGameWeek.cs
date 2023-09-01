using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;

namespace FplBot.Formatting;

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
        catch (HttpRequestException hre) when (LogWarning(hre, gw, leagueId))
        {
            return Enumerable.Empty<Transfer>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Enumerable.Empty<Transfer>();
        }
    }

    private bool LogWarning(HttpRequestException hre, int gw, int leagueId)
    {
        _logger.LogWarning("Could not get transfers in {GW} for {LeagueId}", gw, leagueId);
        return hre.StatusCode == HttpStatusCode.NotFound;
    }

    public record TransfersMessage(string Message);

    public record TransfersPayload(IEnumerable<TransfersMessage> Messages)
    {
        public int GetTotalCharCount()
        {
            return Messages.Sum(c => c.Message.Length);
        }
    };

    public async Task<TransfersPayload> GetTransferMessages(int gw, int leagueId, bool includeExternalLinks = true)
    {
        if (gw < 2)
        {
            return new TransfersPayload(new List<TransfersMessage> { new("No transfers are made the first gameweek.") });
        }

        var leagueTask = _leagueClient.GetClassicLeague(leagueId);
        var settingsTask = _globalSettingsClient.GetGlobalSettings();

        var league = await leagueTask;
        var settings = await settingsTask;

        var sb = new List<TransfersMessage>
        {
            new($"Transfers made for gameweek {gw}:\n\n")
        };

        var didNoTransfers = new ConcurrentBag<ClassicLeagueEntry>();

        await Task.WhenAll(league.Standings.Entries
            .OrderBy(x => x.Rank)
            .Select(entry => GetTransfersTextForEntry(entry, gw, settings.Players, includeExternalLinks))
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
                    sb.Add(new (entryTransfers.Text));
                }
            }));

        if (didNoTransfers.Count > 10)
        {
            sb.Add(new ($"\nThe {didNoTransfers.Count} others saved their transfer 😴"));
        }
        else if (didNoTransfers.Count == 1)
        {
            var who = didNoTransfers.Single();
            var namedWho = includeExternalLinks ? who.GetEntryLink(gw) : who.EntryName;
            sb.Add(new($"\n{namedWho} saved their transfer 😴"));
        }
        else if (didNoTransfers.Count > 0)
        {
            string @join = didNoTransfers.Select(x => {
                var namedWho = includeExternalLinks ? x.GetEntryLink(gw) : x.EntryName;
                return namedWho;
            }).Join();
            sb.Add(new($"\n{@join} saved their transfer 😴"));
        }

        return new TransfersPayload(sb);
    }

    public async Task<string> GetTransfersByGameweekTexts(int gw, int leagueId, bool includeExternalLinks = true)
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
            .Select(entry => GetTransfersTextForEntry(entry, gw, settings.Players, includeExternalLinks))
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
            sb.Append($"\nThe {didNoTransfers.Count} others saved their transfer 😴");
        }
        else if (didNoTransfers.Count == 1)
        {
            var who = didNoTransfers.Single();
            var namedWho = includeExternalLinks ? who.GetEntryLink(gw) : who.EntryName;
            sb.Append($"\n{namedWho} saved their transfer 😴");
        }
        else if (didNoTransfers.Count > 0)
        {
            string @join = didNoTransfers.Select(x => {
                var namedWho = includeExternalLinks ? x.GetEntryLink(gw) : x.EntryName;
                return namedWho;
            }).Join();
            sb.Append($"\n{@join} saved their transfer 😴");
        }

        return sb.ToString();
    }


    private async Task<EntryTranfers> GetTransfersTextForEntry(ClassicLeagueEntry entry, int gameweek, ICollection<Player> players, bool includeExternaLinks)
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
                string entryLinkOrName = includeExternaLinks ? entry.GetEntryLink(gameweek) : entry.EntryName;
                if (wildcardPlayed)
                {
                    sb.Append($"{entryLinkOrName} threw a WILDCAAAAARD 🔥🔥🔥\n");
                }
                else if (freeHitPlayed)
                {
                    sb.Append($"{entryLinkOrName} went all in with the Free Hit chip:\n");
                }
                else
                {
                    var transferCostString = transferCost > 0 ? $" (-{transferCost} pts)" : "";
                    sb.Append($"{entryLinkOrName} transferred{transferCostString}:\n");
                }

                if (wildcardPlayed || freeHitPlayed)
                {
                    sb.Append($"   Made use of {transfers.Length} transfers. Final 11:\n");
                    var firstEleven = picks.Picks.OrderBy(p => p.TeamPosition).Take(11);
                    var starters = firstEleven.Select(first11pick => players.SingleOrDefault(x => x.Id == first11pick.PlayerId)).ToList();
                    foreach (var playerGroup in starters.GroupBy(p => p.Position))
                    {
                        var playersInPos = string.Join("  ", playerGroup.Select(p => p.WebName));
                        var player = playerGroup.First();
                        var pos = Formatter.PositionEmoji(player.Position);
                        sb.Append($"      {pos} {playersInPos}\n");
                    }
                }
                else
                {
                    foreach (var entryTransfer in transfers)
                    {
                        sb.Append($"   ▪️{entryTransfer.PlayerTransferredOut} ({Formatter.FormatCurrency(entryTransfer.SoldFor)}) ➡️ {entryTransfer.PlayerTransferredIn} ({Formatter.FormatCurrency(entryTransfer.BoughtFor)})\n");
                    }
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
