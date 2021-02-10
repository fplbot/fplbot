using System;
using System.Collections.Generic;
using System.Linq;

namespace Fpl.Search.Models
{
    public class SearchResult<T> where T : class
    {
        public IReadOnlyCollection<T> ExposedHits { get; }
        public int MaxHits { get; set; }
        public long TotalHits { get; }
        public bool Any() => ExposedHits.Any();
        public long Count => ExposedHits.Count;
        public long HitCountExceedingExposedOnes => TotalHits - ExposedHits.Count;
        public int Page { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalHits / MaxHits);

        public SearchResult(IReadOnlyCollection<T> exposedHits, long totalHits, int page, int maxHits)
        {
            ExposedHits = exposedHits;
            TotalHits = totalHits;
            Page = page;
            MaxHits = maxHits;
        }
    }
}