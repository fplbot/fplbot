using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplTransfersCommandHandler : IHandleMessages
    {
        private readonly FplbotOptions _fplbotOptions;
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly ITransfersClient _transfersClient;
        private readonly IPlayerClient _playerClient;
        private readonly ILeagueClient _leagueClient;
        private readonly IEntryClient _entryClient;
        private readonly IGameweekHelper _gameweekHelper;

        public FplTransfersCommandHandler(
            IEnumerable<IPublisher> publishers,
            IOptions<FplbotOptions> options, 
            ITransfersClient transfersClient, 
            IPlayerClient playerClient, 
            ILeagueClient leagueClient,
            IEntryClient entryClient,
            IGameweekHelper gameweekHelper)
        {
            _fplbotOptions = options.Value;
            _publishers = publishers;
            _transfersClient = transfersClient;
            _playerClient = playerClient;
            _leagueClient = leagueClient;
            _entryClient = entryClient;
            _gameweekHelper = gameweekHelper;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var leagueTask = _leagueClient.GetClassicLeague(_fplbotOptions.LeagueId);
            var playersTask = _playerClient.GetAllPlayers();
            var gameweekTask = _gameweekHelper.ExtractGameweekOrFallbackToCurrent(message.Text, "transfers {gw}");

            var league = await leagueTask;
            var players = await playersTask;
            var gw = await gameweekTask;

            var entryTasks = league.Standings.Entries
                .ToDictionary(entry => entry.Entry, entry => new
                {
                    TransferTask = _transfersClient.GetTransfers(entry.Entry),
                    PickTask = _entryClient.GetPicks(entry.Entry, gw.Value)
                });

            var entryTransfers = new List<EntryTransfer>();
            foreach (var entry in league.Standings.Entries)
            {
                entryTransfers.AddRange((await entryTasks[entry.Entry].TransferTask).Where(x => x.Event == gw).Select(x => new EntryTransfer
                {
                    EntryId = x.Entry,
                    PlayerTransferredOut = GetPlayerName(players, x.ElementOut),
                    PlayerTransferredIn = GetPlayerName(players, x.ElementIn),
                    SoldFor = x.ElementOutCost,
                    BoughtFor = x.ElementInCost
                }));
            }

            var sb = new StringBuilder();
            sb.Append($"Transfers made for gameweek {gw}:\n\n");

            foreach (var entry in league.Standings.Entries.OrderBy(x => x.Rank))
            {
                sb.Append($"{entry.GetEntryLink(gw)} ");
                var transfersDoneByEntry = entryTransfers.Where(x => x.EntryId == entry.Entry).ToArray();
                if (transfersDoneByEntry.Any())
                {
                    var picks = await entryTasks[entry.Entry].PickTask;
                    var transferCost = picks.EventEntryHistory.EventTransfersCost;
                    var wildcardPlayed = picks.ActiveChip == Constants.ChipNames.Wildcard;
                    var transferCostString = transferCost > 0 ? $" (-{transferCost} pts)" : wildcardPlayed ? " (:fire:wildcard:fire:)" : "";
                    sb.Append($"transferred{transferCostString}:\n");
                    foreach (var entryTransfer in transfersDoneByEntry)
                    {
                        sb.Append($"   {entryTransfer.PlayerTransferredOut} ({Formatter.FormatCurrency(entryTransfer.SoldFor)}) :arrow_right: {entryTransfer.PlayerTransferredIn} ({Formatter.FormatCurrency(entryTransfer.BoughtFor)})\n");
                    }
                }
                else
                {
                    sb.Append("made no transfers :shrug:\n");
                }
            }

            var messageToSend = sb.ToString();

            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = messageToSend
                });
            }

            return new HandleResponse(messageToSend);
        }

        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("transfers {GW/''}", "Displays each team's transfers");
        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("transfers");
        public bool ShouldShowInHelp => true;

        private static string GetPlayerName(IEnumerable<Player> players, int playerId)
        {
            var player = players.SingleOrDefault(x => x.Id == playerId);
            return player != null ? $"{player.FirstName} {player.SecondName}" : "";
        }

        private class EntryTransfer
        {
            public int EntryId { get; set; }
            public string PlayerTransferredOut { get; set; }
            public string PlayerTransferredIn { get; set; }
            public int SoldFor { get; set; }
            public int BoughtFor { get; set; }
        }
    }
}
