using Fpl.Client;
using Fpl.Client.Clients;
using Fpl.Search;
using Fpl.Search.Indexing;
using Fpl.Search.Searching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fpl.SearchConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var options = new Options();
            var esClient = new ElasticClient(new ConnectionSettings(new Uri(options.Value.IndexUri)));
            var indexingClient = new IndexingClient(esClient, logger);
            var searchClient = new SearchClient(esClient, logger, options);
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            });
            FplClientOptionsConfigurator.SetupFplClient(httpClient);
            
            var leagueClient = new LeagueClient(httpClient);
            var indexingService = new IndexingService(indexingClient, 
                new EntryIndexProvider(leagueClient, logger, options), 
                new LeagueIndexProvider(leagueClient, new SimpleLeagueIndexBookmarkProvider(), logger, options), logger);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => indexingService.Cancel();

            Console.WriteLine("You can either \"index <type>\" or \"searchentry/searchleague <term>\"");

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

    internal class Options : IOptions<SearchOptions>
    {
        public SearchOptions Value => new SearchOptions
        {
            EntriesIndex = "test-entries",
            LeaguesIndex = "test-leagues",
            IndexUri = "http://localhost:9200/",
            Username = "-",
            Password = "-",
            ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob = 10000,
            ShouldIndexLeagues = true,
            ShouldIndexEntries = true
        };
    }

    internal class SimpleLeagueIndexBookmarkProvider : IIndexBookmarkProvider
    {
        private string Path = "./bookmark.txt";

        public async Task<int> GetBookmark()
        {
            try
            {
                var txt = await System.IO.File.ReadAllTextAsync(Path);

                return int.TryParse(txt, out int bookmark) ? bookmark : 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }

        public Task SetBookmark(int bookmark)
        {
            try
            {
                Console.WriteLine($"Setting bookmark at {bookmark}.");
                return System.IO.File.WriteAllTextAsync(Path, bookmark.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Task.CompletedTask;
            }
        }
    }
}
