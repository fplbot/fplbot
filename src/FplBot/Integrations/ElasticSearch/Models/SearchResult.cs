namespace Fpl.Search.Models;

public class SearchResult<T>(IReadOnlyCollection<T> exposedHits, long totalHits, int page, int maxHits)
    where T : class
{
    public IReadOnlyCollection<T> ExposedHits { get; } = exposedHits;
    public int MaxHits { get; set; } = maxHits;
    public long TotalHits { get; } = totalHits;
    public bool Any() => ExposedHits.Any();
    public long Count => ExposedHits.Count;
    public long HitCountExceedingExposedOnes => TotalHits - ExposedHits.Count;
    public int Page { get; set; } = page;
    public int TotalPages => (int)Math.Ceiling((double)TotalHits / MaxHits);
}
