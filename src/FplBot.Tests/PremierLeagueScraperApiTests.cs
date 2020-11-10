using System.Net.Http;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot;
using Xunit;

namespace FplBot.Tests
{
    public class PremierLeagueScraperApiTests
    {
        [Fact(Skip = "Integration")]
        public async Task GetMatchWithLineups_GetsLineups()
        {
            var client = new PremierLeagueScraperApi(new HttpClient());
            var matchDetails = await client.GetMatchDetails(58972); // https://www.premierleague.com/match/58972 mci - lfc 1-1, match from the past containing lineups
            Assert.NotNull(matchDetails);
            Assert.True(matchDetails.HasTeams());
            Assert.True(matchDetails.HasLineUps());
        }
        
        [Fact(Skip = "Integration")]
        public async Task GetMatchWithoutLineups_GetsEmptyLineups()
        {
            var client = new PremierLeagueScraperApi(new HttpClient());
            var matchDetails = await client.GetMatchDetails(59272); // https://www.premierleague.com/match/59272 mci - eve (to be played May 23rd 2021)
            Assert.NotNull(matchDetails);
            Assert.True(matchDetails.HasTeams());
            Assert.False(matchDetails.HasLineUps());
        }
    }
}