using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Options;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class CaptainsByGameWeek : ICaptainsByGameWeek
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IEntryClient _entryClient;
        private readonly IPlayerClient _playerClient;
        private readonly ILeagueClient _leagueClient;
        
        public CaptainsByGameWeek(IOptions<FplbotOptions> options, IEntryClient entryClient, IPlayerClient playerClient, ILeagueClient leagueClient)
        {
            _options = options;
            _entryClient = entryClient;
            _playerClient = playerClient;
            _leagueClient = leagueClient;
        }
        
        public async Task<string> GetCaptainsByGameWeek(int gameweek)
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
    }
}