namespace Fpl.Search.Indexing;

public interface IIndexProvider<T> where T : class
{
    string IndexName { get; }
    Task<int> StartIndexingFrom { get; }
    Task Init();
    Task<(T[], bool)> GetBatchToIndex(int i, int batchSize);
}
