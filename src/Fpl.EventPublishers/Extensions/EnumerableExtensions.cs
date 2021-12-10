namespace Fpl.EventPublishers.Extensions;

public static class EnumerableExtensions
{

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Where(x => x != null);
    }

    public static T[] MaterializeToArray<T>(this IEnumerable<T> enumerable)
    {
        return enumerable as T[] ?? enumerable.ToArray();
    }
}
