using Fpl.Client;
using Fpl.Client.Clients;
using Fpl.Search;
using Fpl.Search.Indexing;
using Fpl.Search.Searching;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.SearchConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var options = new SearchOptions
            {
                IndexUri = "http://localhost:9200/"
            };
            
            var logger = new ConsoleLogger();
            var indexingClient = new IndexingClient(logger, options);
            var searchClient = new SearchClient(logger, options);
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            });
            FplClientOptionsConfigurator.SetupFplClient(httpClient);
            
            var leagueClient = new LeagueClient(httpClient);
            var indexingService = new IndexingService(indexingClient, 
                new EntryIndexProvider(leagueClient, logger), 
                new LeagueIndexProvider(leagueClient, logger), logger);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => indexingService.Cancel();

            Console.WriteLine("You can either \"index <type>\" or \"search <term>\"");

            var command = Console.ReadLine()?.ToLower();

            if (command == null)
            {
                Console.WriteLine("Wrong command");
            }
            else
            {
                if (command == "index entries")
                {
                    await indexingService.IndexEntries();
                }
                else if (command == "index leagues")
                {
                    await indexingService.IndexLeagues();
                }
                else if (command.StartsWith("searchentry"))
                {
                    var term = command.Substring(command.IndexOf(" ")).Trim();
                    var result = await searchClient.SearchForEntry(term, 10);
                    Console.WriteLine($"Top {result.Count} hits:\n{string.Join("\n", result.Select(x => $"{x.TeamName} ({x.RealName}) - {x.Entry}"))}\n");
                }
                else if (command.StartsWith("searchleague"))
                {
                    var term = command.Substring(command.IndexOf(" ")).Trim();
                    var result = await searchClient.SearchForLeague(term, 10);
                    Console.WriteLine($"Top {result.Count} hits:\n{string.Join("\n", result.Select(x => $"{x.Name} - {x.Id} (Admin: {x.AdminEntry}"))}\n");
                }
            }
        }
    }

    internal class ConsoleLogger : ILogger<IndexingClient>, ILogger<SearchClient>, ILogger<IndexProviderBase>
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
                Console.WriteLine(formatter(state, exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}
