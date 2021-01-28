using System;
using System.Threading.Tasks;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fpl.SearchConsole.Commands.Handlers
{
    internal static class SearchHandler
    {
        public static async Task SearchDataInElastic(string type, string term, IHost host)
        {
            var serviceProvider = host.Services;
            var searchClient = serviceProvider.GetRequiredService<ISearchService>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            logger.LogInformation($"Searching {type} for {term}");
            var metaData = new SearchMetaData {Actor = Environment.MachineName, Client = QueryClient.Console};

            switch (type)
            {
                case "entries":
                    var entryResult = await searchClient.SearchForEntry(term, 100, metaData);
                    OutputRenderer.RenderEntriesTable(entryResult);
                    break;
                case "leagues":
                    var leagueResult = await searchClient.SearchForLeague(term, 100, metaData, "no");
                    OutputRenderer.RenderLeaguesTable(leagueResult);
                    break;
            }
        }
    }
}