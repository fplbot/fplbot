using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public static class ClientHelper
    {
        public static async Task<T[]> PolledRequests<T>(Func<Task<T>[]> requests, ILogger logger, int retries = 5)
        {
            var j = 0;
            while (j < retries)
            {
                try
                {
                    return await Task.WhenAll(requests());
                }
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    j++;
                    logger.LogWarning($"Got 429, waiting for {2 * j} seconds");
                    await Task.Delay(2000 * j);
                }
            }
            throw new Exception($"Unable to run requests after {retries} retries");
        }
    }
}