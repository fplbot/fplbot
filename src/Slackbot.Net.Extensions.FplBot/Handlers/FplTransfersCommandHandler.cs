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

            var sb = new StringBuilder();
            sb.Append($"Transfers made for gameweek {gw}:\n\n");

            await Task.WhenAll(league.Standings.Entries
                .OrderBy(x => x.Rank)
                .Select(entry => GetTransfersTextForEntry(entry, gw.Value, players))
                .ToArray()
                .ForEach(async task => sb.Append(await task)));

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

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("transfers");
        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("transfers {GW/''}", "Displays each team's transfers");
        public bool ShouldShowInHelp => true;
    }
}
