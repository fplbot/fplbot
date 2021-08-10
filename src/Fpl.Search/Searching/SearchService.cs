using System;
using System.Collections.Generic;
using Fpl.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace Fpl.Search.Searching
{
    public class SearchService : ISearchService
    {
        private readonly IElasticClient _elasticClient;
        private readonly IMessageSession _messageSession;
        private readonly ILogger<SearchService> _logger;
        private readonly SearchOptions _options;
        private readonly Regex _adminCountryRegex = new Regex("^[a-zA-Z]{2}$");

        public SearchService(
            IElasticClient elasticClient,
            IMessageSession messageSession,
            ILogger<SearchService> logger,
            IOptions<SearchOptions> options)
        {
            _elasticClient = elasticClient;
            _messageSession = messageSession;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<SearchResult<EntryItem>> SearchForEntry(string query, int page, int maxHits, SearchMetaData metaData)
        {
            var response = await _elasticClient.SearchAsync<EntryItem>(x => x
                .Index(_options.EntriesIndex)
                .From(page * maxHits)
                .Size(maxHits)
                .Query(q =>
                    q.Boosting(a => a
                        .Positive(p =>
                            p.MultiMatch(m => m
                                .Fields(f =>
                                    f.Field(y => y.RealName, 1.5).Field(y => y.TeamName).Field(y => y.Alias).Field(y => y.Description, 0.5))
                                .Query(query)
                                .Fuzziness(Fuzziness.Auto)
                            ))
                        .NegativeBoost(0.3)
                        .Negative(p => !p.Exists(e => e.Field("verifiedType")))))
                .Sort(sd => sd
                    .Descending(SortSpecialField.Score)
                    .Ascending(SortSpecialField.DocumentIndexOrder))
                .Preference(metaData?.Actor)
            );

            _logger.LogInformation("Entry search for {query} returned {returned} of {hits} hits.", query, response.Hits.Count, response.Total);

            await _messageSession.SendLocal(new IndexQuery(DateTime.UtcNow, query, page, _options.EntriesIndex, null, response.Total, response.Took, metaData?.Client.ToString(), metaData?.Team, metaData?.FollowingFplLeagueId, metaData?.Actor));

            return new SearchResult<EntryItem>(response.Hits.Select(h => h.Source).ToArray(), response.Total, page, maxHits);
        }

        public async Task<EntryItem> GetEntry(int id)
        {
            var response = await _elasticClient.GetAsync<EntryItem>(id, desc => desc.Index(_options.EntriesIndex));

            return response.Found ? response.Source : null;
        }

        public async Task<SearchResult<LeagueItem>> SearchForLeague(string query, int page, int maxHits, SearchMetaData metaData, string countryToBoost = null)
        {
            var response = await _elasticClient.SearchAsync<LeagueItem>(x => x
                .Index(_options.LeaguesIndex)
                .From(page * maxHits)
                .Size(maxHits)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Field(y => y.Name, 1.5).Field(y => y.AdminName))
                        .Query(query)
                        .Fuzziness(Fuzziness.Auto))
                )
                .Sort(GetLeagueSortDescriptor(countryToBoost))
                .Preference(metaData?.Actor)
            );

            _logger.LogInformation("League search for {query} returned {returned} of {hits} hits.", query, response.Hits.Count, response.Total);

            await _messageSession.SendLocal(new IndexQuery(DateTime.UtcNow, query, page, _options.LeaguesIndex, countryToBoost, response.Total, response.Took, metaData?.Client.ToString(), metaData?.Team, metaData?.FollowingFplLeagueId, metaData?.Actor));

            return new SearchResult<LeagueItem>(response.Hits.Select(h => h.Source).ToArray(), response.Total, page, maxHits);
        }

        private Func<SortDescriptor<LeagueItem>, IPromise<IList<ISort>>> GetLeagueSortDescriptor(string countryToBoost)
        {
            if (countryToBoost != null && _adminCountryRegex.IsMatch(countryToBoost))
            {
                countryToBoost = countryToBoost.ToUpper();
                return sd => sd
                    .Descending(SortSpecialField.Score)
                    .Script(descriptor => descriptor
                        .Type("number")
                        .Order(SortOrder.Descending)
                        .Script(scriptDescriptor => scriptDescriptor
                            .Source("doc['adminCountry.keyword'].value == params.country ? 1 : 0")
                            .Params(p => p.Add("country", countryToBoost))
                            .Lang("painless")))
                    .Ascending(SortSpecialField.DocumentIndexOrder);
            }

            return sd => sd
                .Descending(SortSpecialField.Score)
                .Ascending(SortSpecialField.DocumentIndexOrder);
        }

        public async Task<SearchResult<dynamic>> SearchAny(string query, int page, int maxHits, SearchMetaData metaData, SearchType searchType = SearchType.All)
        {
            var response = await _elasticClient.SearchAsync<ILazyDocument>(s => s
                .Index(GetIndexPatternToSearch(searchType))
                .From(page * maxHits)
                .Size(maxHits)
                .Query(q =>
                {
                // https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-boosting-query.html#boosting-top-level-params
                q.Boosting(a => a
                    .Positive(p =>
                    {
                        return p.MultiMatch(dog => dog
                            .Fields(f => f
                                .Field("realName", 15) // entry
                                .Field("name", 1) // league
                                .Field("teamName", 3) // league
                                .Field("adminName", 5) // league
                                .Field("adminTeamName")) // league
                            .Query(string.IsNullOrEmpty(query) ? "*" : query)
                        .Fuzziness(Fuzziness.Auto));
                    })
                    .Negative(p => !p.Exists(e => e.Field("verifiedType")))
                    .NegativeBoost(0.1));
                    return q;
                })
                .Sort(sd => sd
                    .Descending(SortSpecialField.Score)
                    .Ascending(SortSpecialField.DocumentIndexOrder)
                )
                .Preference(metaData?.Actor));

            if (!response.IsValid)
                throw new Exception(response.DebugInformation, response.OriginalException);

            return new SearchResult<dynamic>(response.Hits.Select(h =>
            {
                if (h.Index == _options.EntriesIndex)
                {
                    return new SearchContainer
                    {
                        Type = "entry",
                        Source = h.Source.As<EntryItem>()
                    };
                }

                if (h.Index == _options.LeaguesIndex)
                    return new SearchContainer
                    {
                        Type = "league",
                        Source = h.Source.As<LeagueItem>()
                    };
                return new SearchContainer {Source = h.Source};
            }).ToArray(), response.Total, page, maxHits);
        }

        private string GetIndexPatternToSearch(SearchType searchType)
        {
            return searchType switch
            {
                SearchType.All => $"{_options.EntriesIndex},{_options.LeaguesIndex}",
                SearchType.Entries => _options.EntriesIndex,
                SearchType.Leagues => _options.LeaguesIndex,
                _ => throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null)
            };
        }
    }

    public class SearchContainer
    {
        public object Source { get; set; }
        public string Type { get; set; }
    }

    public interface ISearchService
    {
        Task<SearchResult<EntryItem>> SearchForEntry(string query, int page, int maxHits, SearchMetaData metaData);
        Task<EntryItem> GetEntry(int id);
        Task<SearchResult<LeagueItem>> SearchForLeague(string query, int page, int maxHits, SearchMetaData metaData, string countryToBoost = null);
        Task<SearchResult<dynamic>> SearchAny(string query, int page, int maxHits, SearchMetaData metaData, SearchType searchType = SearchType.All);
    }
}
