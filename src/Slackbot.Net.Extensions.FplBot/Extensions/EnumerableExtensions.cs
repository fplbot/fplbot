
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
            foreach (var item in enumerable.MaterializeToArray())
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

        public static T[] MaterializeToArray<T>(this IEnumerable<T> enumerable)
        {
            return enumerable as T[] ?? enumerable.ToArray();
        }

        public static T GetRandom<T>(this IEnumerable<T> enumerable)
        {
            var array = enumerable.MaterializeToArray();
            if (!array.Any())
            {
                return default;
            }

            if (array.Length == 1)
            {
                return array.Single();
            }

            var random = new Random();
            return array[random.Next(0, array.Length)];
        }

        public static string Join(
            this IEnumerable<string> enumerable, 
            string separator = ", ",
            string lastSeparator = " and ")
        {
            if (enumerable == null)
            {
                return string.Empty;
            }

            var array = enumerable.MaterializeToArray();

            if (!array.Any())
            {
                return string.Empty;
            }
            if (array.Length == 1)
            {
                return array.Single();
            }

            return string.Join(separator, array.Take(array.Length - 1)) + lastSeparator + array.Last();
        }
    }
}
