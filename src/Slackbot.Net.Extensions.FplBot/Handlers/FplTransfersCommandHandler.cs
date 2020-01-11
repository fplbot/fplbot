using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplTransfersCommandHandler : IHandleMessages
    {
        private readonly FplbotOptions _fplbotOptions;
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly ITransfersClient _transfersClient;
        private readonly IPlayerClient _playerClient;
        private readonly ILeagueClient _leagueClient;
        private readonly IGameweekHelper _gameweekHelper;

        public FplTransfersCommandHandler(
            IEnumerable<IPublisher> publishers,
            IOptions<FplbotOptions> options, 
            ITransfersClient transfersClient, 
            IPlayerClient playerClient, 
            ILeagueClient leagueClient,
            IGameweekHelper gameweekHelper)
        {
            _fplbotOptions = options.Value;
            _publishers = publishers;
            _transfersClient = transfersClient;
            _playerClient = playerClient;
            _leagueClient = leagueClient;
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

            var transferTasks = league.Standings.Entries
                .Select(entry => _transfersClient.GetTransfers(entry.Entry))
                .ToArray();

            var entryTransfers = new List<EntryTransfer>();
            foreach (var transferTask in transferTasks)
            {
                entryTransfers.AddRange((await transferTask).Where(x => x.Event == gw).Select(x => new EntryTransfer
                {
                    EntryId = x.Entry,
                    PlayerTransferredOut = GetPlayerName(players, x.ElementOut),
                    PlayerTransferredIn = GetPlayerName(players, x.ElementIn)
                }));
            }

            var sb = new StringBuilder();
            sb.Append($"Transfers for GW{gw}:\n\n");

            foreach (var entry in league.Standings.Entries.OrderBy(x => x.Rank))
            {
                sb.Append($"<https://fantasy.premierleague.com/entry/{entry.Entry}/event/{gw}|{entry.EntryName}> ");
                var transfersDoneByEntry = entryTransfers.Where(x => x.EntryId == entry.Entry).ToArray();
                if (transfersDoneByEntry.Any())
                {
                    sb.Append("transferred:\n");
                    foreach (var entryTransfer in transfersDoneByEntry)
                    {
                        sb.Append($"   {entryTransfer.PlayerTransferredOut} :arrow_right: {entryTransfer.PlayerTransferredIn}\n");
                    }
                }
                else
                {
                    sb.Append("did no transfers\n");
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

        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("transfers {GW/''}", "Displays each team's ");
        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("transfers");
        public bool ShouldShowInHelp => true;

        private string GetPlayerName(ICollection<Player> players, int playerId)
        {
            var player = players.SingleOrDefault(x => x.Id == playerId);
            return player != null ? $"{player.FirstName} {player.SecondName}" : "";
        }

        class EntryTransfer
        {
            public int EntryId { get; set; }
            public string PlayerTransferredOut { get; set; }
            public string PlayerTransferredIn { get; set; }
        }
    }
}
