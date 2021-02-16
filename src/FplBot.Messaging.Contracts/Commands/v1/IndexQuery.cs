using System;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public class IndexQuery : ICommand
    {
        public IndexQuery(DateTime TimeStamp,
            string Query,
            int Page,
            string QueriedIndex,
            string BoostedCountry,
            long TotalHits,
            long ResponseTimeMs,
            string Client,
            string Team,
            string FollowingFplLeagueId,
            string Actor)
        {
            this.TimeStamp = TimeStamp;
            this.Query = Query;
            this.Page = Page;
            this.QueriedIndex = QueriedIndex;
            this.BoostedCountry = BoostedCountry;
            this.TotalHits = TotalHits;
            this.ResponseTimeMs = ResponseTimeMs;
            this.Client = Client;
            this.Team = Team;
            this.FollowingFplLeagueId = FollowingFplLeagueId;
            this.Actor = Actor;
        }

        public DateTime TimeStamp { get; }

        public string Query { get; }

        public int Page { get; }

        public string QueriedIndex { get; }

        public string BoostedCountry { get; }

        public long TotalHits { get; }

        public long ResponseTimeMs { get; }

        public string Client { get; }

        public string Team { get; }

        public string FollowingFplLeagueId { get; }

        public string Actor { get; }
    }
}