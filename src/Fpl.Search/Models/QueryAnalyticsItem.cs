using System;

namespace Fpl.Search.Models
{
    public class QueryAnalyticsItem : IIndexableItem
    {
        public DateTime TimeStamp { get; set; }
        public string Query { get; set; }
        public long TotalHits { get; set; }
        public QueryClient? Client { get; set; }
        public string Team { get; set; }
        public string Actor { get; set; }
    }

    public enum QueryClient
    {
        Slack = 0,
        Web = 1,
        Console = 2
    }
}