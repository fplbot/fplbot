using System.Collections.Concurrent;

namespace FplBot.WebApi.Slack.Helpers;

internal static class SearchHelper
{
    private const int LevenshteinDistanceThreshold = 3;

    public static SearchResult<T>? Find<T>(IEnumerable<T> collection, string input, params Func<T, ISearchableProperty>[] searchProperties)
    {
        var searchPropertiesWithPri = searchProperties.Select((prop, idx) => new {Pri = idx, Prop = prop}).ToArray();

        var searchResultsForProps = new ConcurrentBag<SearchResultWithPri<T>>();
        Parallel.ForEach(searchPropertiesWithPri, x =>
        {
            searchResultsForProps.Add(new SearchResultWithPri<T>(x.Pri, Find(collection, input, x.Prop)));
        });

        var searchResultsForPropsOrderedByPri = searchResultsForProps.OrderBy(x => x.Pri).ToArray();

        var perfectMatch = searchResultsForPropsOrderedByPri.FirstOrDefault(x => x.SearchResult.LevenshteinDistance == 0);
        if (perfectMatch != null)
        {
            return perfectMatch.SearchResult;
        }

        foreach (var searchResult in searchResultsForPropsOrderedByPri)
        {
            if (searchResult.SearchResult.LevenshteinDistance <= LevenshteinDistanceThreshold)
            {
                return searchResult.SearchResult;
            }
        }

        return null;
    }

    public static SearchResult<T> Find<T>(IEnumerable<T> collection, string input, Func<T, ISearchableProperty> searchProperties)
    {
        var normalizedInput = input.ToLower();

        var lev = new Fastenshtein.Levenshtein(normalizedInput);

        var lowestDistance = int.MaxValue;
        T? currentWinner = default;

        foreach (var item in collection)
        {
            foreach (var searchProperty in searchProperties(item).AsStrings)
            {
                var termToMatchAgainst = searchProperty.ToLower();
                if (termToMatchAgainst == normalizedInput)
                {
                    return new SearchResult<T>(item, 0);
                }

                var distance = lev.DistanceFrom(termToMatchAgainst);
                if (distance >= lowestDistance) continue;

                lowestDistance = distance;
                currentWinner = item;
            }
        }

        return new SearchResult<T>(currentWinner, lowestDistance);
    }

    private class SearchResultWithPri<T>(int pri, SearchResult<T> searchResult)
    {
        public int Pri { get; } = pri;
        public SearchResult<T> SearchResult { get; } = searchResult;
    }
}

internal class SearchResult<T>(T? item, int levenshteinDistance)
{
    public T? Item { get; } = item;
    public int LevenshteinDistance { get; } = levenshteinDistance;
}

internal interface ISearchableProperty
{
    string[] AsStrings { get; }
}

internal class SearchableProperty(string property) : ISearchableProperty
{
    public string[] AsStrings => new[] { property };
}

internal class SearchablePropertyCollection(string[] properties) : ISearchableProperty
{
    public string[] AsStrings { get; } = properties;
}

internal static class SearchablePropertyExtensions
{
    public static ISearchableProperty Searchable(this string s)
    {
        return new SearchableProperty(s);
    }
    public static ISearchableProperty Searchable(this string[] s)
    {
        return new SearchablePropertyCollection(s);
    }
}
