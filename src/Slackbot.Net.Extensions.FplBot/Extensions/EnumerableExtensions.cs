using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Extensions
{
    public static class EnumerableExtensions
    {
        public static async Task ForEach<T>(this IEnumerable<T> enumerable, Func<T, Task> func)
        {
            foreach (var item in enumerable)
            {
                await func(item);
            }
        }

        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Where(x => x != null);
        }
    }
}
