using Fpl.Search;
using Microsoft.Extensions.Options;

namespace Fpl.SearchConsole
{
    internal class Options : IOptions<SearchOptions>
    {
        public SearchOptions Value => new SearchOptions
        {
            EntriesIndex = "test-entries",
            LeaguesIndex = "test-leagues",
            AnalyticsIndex = "test-analytics",
            IndexUri = "http://localhost:9200/",
            Username = "-",
            Password = "-",
            ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob = 10000,
            ShouldIndexLeagues = true,
            ShouldIndexEntries = true
        };
    }
}