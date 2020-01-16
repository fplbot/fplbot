using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal static class SearchHelper
    {
        private const int LevenshteinDistanceThreshold = 3;

        public static SearchResult<T> Find<T>(IEnumerable<T> collection, string input, params Func<T, string>[] searchProperties)
        {
            var searchPropertiesWithPri = searchProperties.Select((prop, idx) => new {Pri = idx, Prop = prop}).ToArray();
            
            var searchResultsForProps = new ConcurrentBag<SearchResultWithPri<T>>();
            Parallel.ForEach(searchPropertiesWithPri, x =>
            {
                searchResultsForProps.Add(new SearchResultWithPri<T>(x.Pri, Find(collection, input, x.Prop)));
            });

            var perfectMatch = searchResultsForProps.FirstOrDefault(x => x.SearchResult.LevenshteinDistance == 0);
            if (perfectMatch != null)
            {
                return perfectMatch.SearchResult;
            }
            
            foreach (var searchResult in searchResultsForProps.OrderBy(x => x.Pri).ToArray())
            {
                if (searchResult.SearchResult.LevenshteinDistance <= LevenshteinDistanceThreshold)
                {
                    return searchResult.SearchResult;
                }
            }

            return null;
        }

        public static SearchResult<T> Find<T>(IEnumerable<T> collection, string input, Func<T, string> searchProperty)
        {
            var normalizedInput = input.ToLower();

            var lev = new Fastenshtein.Levenshtein(normalizedInput);

            var lowestDistance = int.MaxValue;
            var currentWinner = default(T);

            foreach (var item in collection)
            {
                var termToMatchAgainst = searchProperty(item).ToLower();
                if (termToMatchAgainst == normalizedInput)
                {
                    return new SearchResult<T>(item, 0);
                }

                var distance = lev.DistanceFrom(termToMatchAgainst);
                if (distance >= lowestDistance) continue;

                lowestDistance = distance;
                currentWinner = item;
            }

            return new SearchResult<T>(currentWinner, lowestDistance);
        }

        private class SearchResultWithPri<T>
        {
            public int Pri { get; }
            public SearchResult<T> SearchResult { get; }
   
            public SearchResultWithPri(int pri, SearchResult<T> searchResult)
            {
                Pri = pri;
                SearchResult = searchResult;
            }
        }
    }

    internal class SearchResult<T>
    {
        public T Item { get; }
        public int LevenshteinDistance { get; }

        public SearchResult(T item, int levenshteinDistance)
        {
            Item = item;
            LevenshteinDistance = levenshteinDistance;
        }
    }
}
