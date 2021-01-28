using System;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Search.Indexing;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Fpl.SearchConsole
{
    internal static class CommandHandlers
    {
        public static async Task IndexDataIntoElastic(string type, IHost host, CancellationToken token)
        {
            var serviceProvider = host.Services;
            var indexer = serviceProvider.GetRequiredService<IIndexingService>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            logger.LogInformation($"Indexing {type}");

            switch (type)
            {
                case "entries":
                    await AnsiConsole.Status()
                        .AutoRefresh(true)
                        .Spinner(Spinner.Known.Runner)
                        .SpinnerStyle(Style.Parse("green bold"))
                        .StartAsync("Indexing entries...", async ctx =>
                        {
                            await indexer.IndexEntries(token, page =>
                            {
                                ctx.Status = $"Page {page} indexed";
                            });
                        });
                    break;
                case "leagues":
                    await AnsiConsole.Status()
                        .AutoRefresh(true)
                        .Spinner(Spinner.Known.Runner)
                        .SpinnerStyle(Style.Parse("green bold"))
                        .StartAsync("Indexing entries...", async ctx =>
                        {
                            await indexer.IndexLeagues(token, page =>
                            {
                                ctx.Status = $"Page {page} indexed";
                            });
                        });
                    break;
            }
            
        }

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