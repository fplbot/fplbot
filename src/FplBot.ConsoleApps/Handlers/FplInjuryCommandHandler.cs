using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Workers.Handlers;
using Slackbot.Net.Workers.Publishers;
using SlackConnector.Models;

namespace FplBot.ConsoleApps.Handlers
{
    public class FplInjuryCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IPlayerClient _playerClient;

        public FplInjuryCommandHandler(IEnumerable<IPublisher> publishers, IPlayerClient playerClient, ITeamsClient teamsClient)
        {
            _publishers = publishers;
            _playerClient = playerClient;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {

            var allPlayers = await _playerClient.GetAllPlayers();

            var injuredPlayers = FindInjuredPlayers(allPlayers);

            var textToSend = Formatter.GetInjuredPlayers(injuredPlayers);

            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = textToSend
                });
            }

            if (string.IsNullOrEmpty(textToSend))
            {
                return new HandleResponse("Not found");

            }
            return new HandleResponse(textToSend);
        }

        private static IEnumerable<Player> FindInjuredPlayers(IEnumerable<Player> players)
        {
            return players.Where((p) => p.OwnershipPercentage > 5 && IsInjured(p));
        }

        private static bool IsInjured(Player player)
        {
            return player.ChanceOfPlayingNextRound != "100" && player.ChanceOfPlayingNextRound != null;
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("injuries");
        }

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("injuries", "Henter spillere med skader eid av 5%<");
        }

        public bool ShouldShowInHelp => true;
    }

}
