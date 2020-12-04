using Fpl.Client;
using Fpl.Client.Clients;
using Fpl.Client.Models;
using Fpl.Search;
using Fpl.Search.Indexing;
using Fpl.Search.Models;
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
            
            var indexingClient = new IndexingClient(new ConsoleLogger(), options);
            var searchClient = new SearchClient(new ConsoleLogger(), options);
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            });
            FplClientOptionsConfigurator.SetupFplClient(httpClient);
            
            var leagueClient = new LeagueClient(httpClient);

            
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
                                x.Standings.Entries.Select(y => new EntryItem {Id = y.Id, TeamName = y.EntryName, RealName = y.PlayerName, Entry = y.Entry})).ToArray();

                            await indexingClient.IndexEntries(entries);

                            i += batchSize;

                            if (iteration % 10 == 0)
                                Console.WriteLine($"{DateTime.Now.ToString("T")} indexed page {i}");

                            hasNext = batchOfStandings.All(x => x.Standings.HasNext);
                            iteration++;
                        }
                    } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                }
                else if (command == "index leagues")
                {
                    var i = 1;
                    var batchSize = 8;
                    var iteration = 1;
                    var hasNext = true;

                    do
                    {
                        while (!Console.KeyAvailable && hasNext)
                        {
                            var batchOfLeagues = await GetBatchOfLeagues(i, batchSize, leagueClient);
                            var leagues = batchOfLeagues.Select(x => new LeagueItem { Id = x.Properties.Id, Name = x.Properties.Name, AdminEntry = x.Properties.AdminEntry}).ToArray();

                            await indexingClient.IndexLeagues(leagues);

                            i += batchSize;

                            if (iteration % 10 == 0)
                                Console.WriteLine($"{DateTime.Now:T} indexed page {i}");

                            hasNext = batchOfLeagues.Count(x => x.Exists) > 1;
                            iteration++;
                        }
                    } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
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

        private static async Task<ClassicLeague[]> GetBatchOfLeagues(int i, int batchSize, LeagueClient leagueClient)
        {
            var retries = 3;
            var j = 0;

            while (j < retries)
            {
                try
                {
                    return await Task.WhenAll(Enumerable.Range(i, batchSize).Select(n => leagueClient.GetClassicLeague(i)));
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
