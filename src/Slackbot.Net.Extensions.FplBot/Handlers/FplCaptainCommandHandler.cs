using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Connections;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplCaptainCommandHandler : IHandleMessages
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IGameweekClient _gameweekClient;
        private readonly IEntryClient _entryClient;
        private readonly IPlayerClient _playerClient;
        private readonly ILeagueClient _leagueClient;
        private readonly BotDetails _botDetails;

        public FplCaptainCommandHandler(IOptions<FplbotOptions> options, IEnumerable<IPublisher> publishers, IGameweekClient gameweekClient, IEntryClient entryClient, IPlayerClient playerClient, ILeagueClient leagueClient, BotDetails botDetails)
        {
            _options = options;
            _publishers = publishers;
            _gameweekClient = gameweekClient;
            _playerClient = playerClient;
            _entryClient = entryClient;
            _leagueClient = leagueClient;
            _botDetails = botDetails;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameWeek = await GetGameweek(message);

            var messageToSend = gameWeek.HasValue ? await GetCaptainPicks(gameWeek.Value) : "Ugyldig gameweek :grimacing:";

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

        private async Task<int?> GetGameweek(SlackMessage message)
        {
            var replacements = new[]
            {
                new {Find = "@fplbot", Replace = ""},
                new {Find = $"<@{_botDetails.Id}>", Replace = ""}, // @fplbot-userid
                new {Find = "captains", Replace = ""}
            };

            var name = message.Text;

            foreach (var set in replacements)
            {
                name = name.Replace(set.Find, set.Replace).Trim();
            }

            if (name != "")
            {
                try
                {
                    return int.Parse(name);
                }
                catch (FormatException)
                {
                    return null;
                }
            }

            var gameweeks = await _gameweekClient.GetGameweeks();
            var currentGw = gameweeks.SingleOrDefault(x => x.IsCurrent)?.Id;

            return currentGw;
        }

        private async Task<string> GetCaptainPicks(int gameweek)
        {
            try
            {
                var league = await _leagueClient.GetClassicLeague(_options.Value.LeagueId);
                var players = await _playerClient.GetAllPlayers();

                var sb = new StringBuilder();

                sb.Append($":boom: *Captain picks for gameweek {gameweek}*\n");

                foreach (var team in league.Standings.Entries)
                {
                    var entry = await _entryClient.GetPicks(team.Entry, gameweek);
                    var captainPick = entry.Picks.SingleOrDefault(pick => pick.IsCaptain);
                    var captain = players.SingleOrDefault(player => player.Id == captainPick.PlayerId);

                    sb.Append($"*{team.EntryName}* - {captain.FirstName} {captain.SecondName}\n");
                }

                return sb.ToString();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return $"Oops: {e.Message}";
            }
        }

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("captains {GW/''}", "Henter kapteinsvalg for liga");
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("captains");
        }

        public bool ShouldShowInHelp => true;
    }
}
