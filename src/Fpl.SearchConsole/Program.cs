using Fpl.Client;
using Fpl.Client.Clients;
using Fpl.Search;
using Fpl.Search.Indexing;
using Fpl.Search.Searching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Search.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fpl.SearchConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await BuildCommandLine().UseHost(
                    _ => Host.CreateDefaultBuilder(args)
                        .ConfigureServices((hostContext, services) => services.AddSearchConsole()))
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }


        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand();
            root.AddCommand(CreateIndexCommand());
            root.AddCommand(CreateSearchCommand());
            return new CommandLineBuilder(root);
        }

        private static Command CreateIndexCommand()
        {
            Argument<string> typeArgument = new("type", "What types of documents you want to index");
            typeArgument.Suggestions.Add("entries");
            typeArgument.Suggestions.Add("leagues");
            var indexCommand = new Command("index", "index data into ElasticSearch")
            {
                typeArgument
            };
            indexCommand.Handler = CommandHandler.Create<string, IHost>(IndexDataIntoElastic);
            return indexCommand;
        }
        
        private static Command CreateSearchCommand()
        {
            Argument<string> typeArgument = new("type", "What types of documents you want to index");
            typeArgument.Suggestions.Add("entries");
            typeArgument.Suggestions.Add("leagues");

            Argument<string> termArgument = new("term", "Search term");
            termArgument.Suggestions.Add("Magnus Carlsen");
            
            var searchCommand = new Command("search", "search data in ElasticSearch")
            {
                typeArgument,
                termArgument
            };
            searchCommand.Handler = CommandHandler.Create<string,string, IHost>(SearchDataInElastic);
            return searchCommand;
        }
        
        private static async Task IndexDataIntoElastic(string type, IHost host)
        {
            var serviceProvider = host.Services;
            var indexer = serviceProvider.GetRequiredService<IndexingService>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            logger.LogInformation($"Indexing {type}");
            switch (type)
            {
                case "entries":
                    await indexer.IndexEntries();
                    break;
                case "leagues":
                    await indexer.IndexLeagues();
                    break;
            }
        }

        private static async Task SearchDataInElastic(string type, string term, IHost host)
        {
            var serviceProvider = host.Services;
            var searchClient = serviceProvider.GetRequiredService<SearchService>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            logger.LogInformation($"Searching in {type} for {term}");
            var metaData = new SearchMetaData {Actor = Environment.MachineName, Client = QueryClient.Console};
            
            switch (type)
            {
                case "entries":
                    var entryResult = await searchClient.SearchForEntry(term, 100, metaData);
                    logger.LogInformation($"Top {entryResult.Count} hits:\n{string.Join("\n", entryResult.ExposedHits.Select(x => $"{x.TeamName} ({x.RealName}) - {x.Entry} {(x.VerifiedType != null ? $"({x.VerifiedType})" : null)}"))}\n");    
                    break;
                case "leagues":
                    var leagueResult = await searchClient.SearchForLeague(term, 100, metaData, "no");
                    logger.LogInformation($"Top {leagueResult.Count} hits:\n{string.Join("\n", leagueResult.ExposedHits.Select(x => $"{x.Name} - {x.Id} (Admin: {x.AdminEntry})"))}\n");
                    break;
            }
        }
    }


    public static class StuffDoer
    {
        public static void DoStuff()
        {
            // Console.WriteLine("You can either \"index <type>\" or \"searchentry/searchleague <term>\"");
            // var command = Console.ReadLine()?.ToLower();
            //
            // if (command == null)
            // {
            //     Console.WriteLine("Wrong command");
            // }
            // else
            // {
            //     if (command == "index entries")
            //     {
            //         await indexingService.IndexEntries();
            //     }
            //     else if (command == "index leagues")
            //     {
            //         await indexingService.IndexLeagues();
            //     }
            //     else if (command.StartsWith("searchentry"))
            //     {
            //         var term = command.Substring(command.IndexOf(" ")).Trim();
            //         var metaData = new SearchMetaData {Actor = Environment.MachineName, Client = QueryClient.Console};
            //         var result = await searchClient.SearchForEntry(term, 100, metaData);
            //         Console.WriteLine(
            //             $"Top {result.Count} hits:\n{string.Join("\n", result.ExposedHits.Select(x => $"{x.TeamName} ({x.RealName}) - {x.Entry} {(x.VerifiedType != null ? $"({x.VerifiedType})" : null)}"))}\n");
            //     }
            //     else if (command.StartsWith("searchleague"))
            //     {
            //         var term = command.Substring(command.IndexOf(" ")).Trim();
            //         var metaData = new SearchMetaData {Actor = Environment.MachineName, Client = QueryClient.Console};
            //         var result = await searchClient.SearchForLeague(term, 100, metaData, "no");
            //         Console.WriteLine(
            //             $"Top {result.Count} hits:\n{string.Join("\n", result.ExposedHits.Select(x => $"{x.Name} - {x.Id} (Admin: {x.AdminEntry})"))}\n");
            //     }
            // }
        }
    }

    public static class ServiceCollectionExtensions
        {
            public static IServiceCollection AddSearchConsole(this IServiceCollection services)
            {
                var logger = new ConsoleLogger();
                var options = new Options();

                var conn = new ConnectionSettings(new Uri(options.Value.IndexUri));
                if (!string.IsNullOrEmpty(options.Value.Username) && !string.IsNullOrEmpty(options.Value.Password))
                {
                    conn.BasicAuthentication(options.Value.Username, options.Value.Password);
                }

                var esClient = new ElasticClient(conn);
                var indexingClient = new IndexingClient(esClient, logger);
                var queryStatsIndexingService = new QueryAnalyticsIndexingService(indexingClient, options, logger);
                var searchClient = new SearchService(esClient, queryStatsIndexingService, logger, options);
                var httpClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip,
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                });
                FplClientOptionsConfigurator.SetupFplClient(httpClient);

                var leagueClient = new LeagueClient(httpClient);
                var entryClient = new EntryClient(httpClient);
                var indexingService = new IndexingService(indexingClient,
                    new EntryIndexProvider(leagueClient, logger, options),
                    new LeagueIndexProvider(leagueClient, entryClient, new SimpleLeagueIndexBookmarkProvider(), logger,
                        options), logger);

                services.AddSingleton(indexingService);
                services.AddSingleton(searchClient);
                return services;
            }
        }


        internal class ConsoleLogger : ILogger<IndexingClient>, ILogger<SearchService>, ILogger<IndexProviderBase>,
            ILogger<QueryAnalyticsIndexingService>
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
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
                AnalyticsIndex = "test-analytics",
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

