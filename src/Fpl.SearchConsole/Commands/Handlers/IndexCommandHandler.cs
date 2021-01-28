using System;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Search.Indexing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Fpl.SearchConsole.Commands.Handlers
{
    internal static class IndexCommandHandler
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
                    await RenderStatus(async ctx => { await indexer.IndexEntries(token, page => { ctx.Status = $"Page {page} of {type} indexed"; }); });
                    break;
                case "leagues":
                    await RenderStatus(async ctx => { await indexer.IndexLeagues(token, page => { ctx.Status = $"Page {page} of {type} indexed"; }); });
                    break;
            }

            async Task RenderStatus(Func<StatusContext, Task> runner)
            {
                await AnsiConsole.Status()
                    .AutoRefresh(true)
                    .Spinner(Spinner.Known.Runner)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync($"Indexing {type}...", runner);
            }
        }
    }
}