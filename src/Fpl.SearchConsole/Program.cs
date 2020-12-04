using Fpl.Client;
using Fpl.Client.Clients;
using Fpl.Client.Models;
using Fpl.Search;
using Fpl.Search.Indexing;
using Fpl.Search.Searching;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Entry = Fpl.Search.Models.Entry;

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
            
            var indexingClient = new IndexingClient(new ConsoleLogger(), options);
            var searchClient = new SearchClient(new ConsoleLogger(), options);
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            });
            FplClientOptionsConfigurator.SetupFplClient(httpClient);

            const string indexA = "entries_a";
            const string indexB = "entries_b";
            var indexAInUse = await indexingClient.IsActiveIndex(indexA);
            var indexCurrentlyInUse = indexAInUse ? indexA : indexB;

            var leagueClient = new LeagueClient(httpClient);

            Console.WriteLine("You can either \"index\" or \"search <term>\"");

            var command = Console.ReadLine()?.ToLower();

            if (command == null)
            {
                Console.WriteLine("Wrong command");
            }
            else
            {
                if (command == "index")
                {
                    var indexToUse = indexAInUse ? indexB : indexA;
                    var indexToDispose = indexAInUse ? indexA : indexB;

                    var i = 1;
                    var batchSize = 8;
                    var iteration = 1;
                    var hasNext = true;
                    do
                    {
                        while (!Console.KeyAvailable && hasNext)
                        {
                            var batchOfStandings = await GetBatchOfStandings(i, batchSize, leagueClient);
                            var entries = batchOfStandings.SelectMany(x =>
                                x.Standings.Entries.Select(y => new Entry {Id = y.Id, TeamName = y.EntryName, RealName = y.PlayerName})).ToArray();

                            await indexingClient.Index(entries, indexToUse);

                            i += batchSize;

                            if (iteration % 10 == 0)
                                Console.WriteLine($"{DateTime.Now.ToString("T")} indexed page {i}");

                            hasNext = batchOfStandings.All(x => x.Standings.HasNext);
                            iteration++;
                        }
                    } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

                    await indexingClient.DisposeIndex(indexToDispose);
                } 
                else if (command.StartsWith("search"))
                {
                    var term = command.Substring(command.IndexOf(" ")).Trim();
                    var result = await searchClient.Search(term, 10, indexCurrentlyInUse);
                    Console.WriteLine($"Top {result.Count} hits:\n{string.Join("\n", result.Select(x => $"{x.TeamName} ({x.RealName}) - {x.Id}"))}\n");
                }
            }
        }

        private static async Task<ClassicLeague[]> GetBatchOfStandings(int i, int batchSize, LeagueClient leagueClient)
        {
            var retries = 3;
            var j = 0;

            while (j < retries)
            {
                try
                {
                    return await Task.WhenAll(Enumerable.Range(i, batchSize).Select(n => leagueClient.GetClassicLeague(314, n)));
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Ran into a 429 (Too Many Requests) at {i}. Waiting 1s before retrying.");
                    await Task.Delay(2000);
                    j++;
                }
            }
            throw new Exception($"Unable to get standings after {retries} retries");
        }
    }

    internal class ConsoleLogger : ILogger<IndexingClient>, ILogger<SearchClient>
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
                Console.WriteLine(formatter(state, exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Error;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}
