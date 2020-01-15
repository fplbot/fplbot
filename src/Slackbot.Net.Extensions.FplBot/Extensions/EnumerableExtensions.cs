using System;
using System.Collections.Generic;
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
    }
}
