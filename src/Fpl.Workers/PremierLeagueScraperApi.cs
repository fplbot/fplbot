using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;
using FplBot.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FplBot.Core
{
    internal class PremierLeagueScraperApi : IGetMatchDetails
    {
        private readonly HttpClient _client;
        private readonly ILogger<PremierLeagueScraperApi> _logger;

        public PremierLeagueScraperApi(HttpClient client, ILogger<PremierLeagueScraperApi> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<MatchDetails> GetMatchDetails(int pulseId)
        {
            try
            {
                var res = await _client.GetStringAsync($"https://www.premierleague.com/match/{pulseId}");
                var context = BrowsingContext.New();
                var document = await context.OpenAsync(req => req.Content(res));
                var fixture = document.QuerySelectorAll("div.mcTabsContainer").First();
                var json = fixture.Attributes.GetNamedItem("data-fixture").Value;
                return JsonConvert.DeserializeObject<MatchDetails>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class MatchDetails
    {
        public int Id { get; set; }
        // The two teams participating
        public IEnumerable<TeamDetails> Teams { get; set; } = new List<TeamDetails>();

        // The lineup
        public IEnumerable<LineupContainer> TeamLists { get; set; } = new List<LineupContainer>();

        public bool HasLineUps()
        {
            return TeamLists != null && TeamLists.All(c => c != null) && TeamLists.All(l => l.HasLineups());
        }

        public bool HasTeams()
        {
            return Teams != null && Teams.Any() && Teams.All(t => t.Team != null);
        }
    }

    public class TeamDetails
    {
        public PulseTeam Team { get; set; }
    }

    public class PulseTeam
    {
        public int Id { get; set; }
        public Club Club { get; set; }
    }

    public class Club
    {
        public string Abbr { get; set; }
    }

    public class LineupContainer
    {
        public int TeamId { get; set; }
        public IEnumerable<PlayerInLineup> Lineup { get; set; } = new List<PlayerInLineup>();

        public Formation Formation { get; set; }

        public bool HasLineups()
        {
            return Lineup != null && Lineup.Any();
        }
    }

    public class Formation
    {
        public string Label { get; set; }
        public IEnumerable<IEnumerable<int>> Players { get; set; }
    }

    public class PlayerInLineup
    {
        public const string MatchPositionGoalie = "G";
        public const string MatchPositionDefender = "D";
        public const string MatchPositionMidfielder = "M";
        public const string MatchPositionForward = "F";

        public int Id { get; set; }
        public string MatchPosition { get; set; }
        public Name Name { get; set; }
        public bool Captain { get; set; }
    }

    public class Name
    {
        public string First { get; set; }
        public string Last { get; set; }
        public string Display { get; set; }

        public override string ToString()
        {
            var names = Display.Split(" ");
            return names.Length == 1 ? Display : $"{string.Join(" ",names[1..])}";
        }
    }
}
