using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fpl.Client
{
    public static class ClientHelper
    {
        public static async Task<T[]> PolledRequests<T>(Func<Task<T>[]> requests, ILogger logger, int retries = 3)
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