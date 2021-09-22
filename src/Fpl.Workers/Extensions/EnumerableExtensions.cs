using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FplBot.Core.Extensions
{
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
