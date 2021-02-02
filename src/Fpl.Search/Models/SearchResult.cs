using System.Collections.Generic;
using System.Linq;

namespace Fpl.Search.Models
{
    public class SearchResult<T> where T : class
    {
        public IReadOnlyCollection<T> ExposedHits { get; }
        public long TotalHits { get; }
        public bool Any() => ExposedHits.Any();
        public long Count => ExposedHits.Count;
        public long HitCountExceedingExposedOnes => TotalHits - ExposedHits.Count;

        public SearchResult(IReadOnlyCollection<T> exposedHits, long totalHits)
        {
            ExposedHits = exposedHits;
            TotalHits = totalHits;
        }
    }
}