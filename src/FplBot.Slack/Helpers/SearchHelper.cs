using System.Collections.Concurrent;

namespace FplBot.Slack.Helpers;

internal static class SearchHelper
{
    private const int LevenshteinDistanceThreshold = 3;

    public static SearchResult<T> Find<T>(IEnumerable<T> collection, string input, params Func<T, ISearchableProperty>[] searchProperties)
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
        var currentWinner = default(T);

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

internal interface ISearchableProperty
{
    string[] AsStrings { get; }
}

internal class SearchableProperty : ISearchableProperty
{
    private readonly string _property;
    public SearchableProperty(string property)
    {
        _property = property;
    }
    public string[] AsStrings => new[] { _property };
}

internal class SearchablePropertyCollection : ISearchableProperty
{
    public SearchablePropertyCollection(string[] properties)
    {
        AsStrings = properties;
    }
    public string[] AsStrings { get; }
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
