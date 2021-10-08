using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FplBot.Formatting
{
    public static class EnumarableExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Where(x => x != null);
        }

        public static async Task ForEach<T>(this IEnumerable<T> enumerable, Func<T, Task> func)
        {
            foreach (var item in enumerable.MaterializeToArray())
            {
                await func(item);
            }
        }

        public static T[] MaterializeToArray<T>(this IEnumerable<T> enumerable)
        {
            return enumerable as T[] ?? enumerable.ToArray();
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
    }
}
