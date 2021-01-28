using System;

namespace Fpl.Search.Models
{
    public class QueryAnalyticsItem : IIndexableItem
    {
        public DateTime TimeStamp { get; set; }
        public string Query { get; set; }
        public string QueriedIndex { get; set; }
        public string BoostedCountry { get; set; }
        public long TotalHits { get; set; }
        public long ResponseTimeMs { get; set; }
        public QueryClient? Client { get; set; }
        public string Team { get; set; }
        public string FollowingFplLeagueId { get; set; }
        public string Actor { get; set; }
    }

    public enum QueryClient
    {
        Slack = 0,
        Web = 1,
        Console = 2
    }
}