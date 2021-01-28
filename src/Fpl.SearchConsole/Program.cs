using Fpl.Client;
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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fpl.SearchConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await BuildCommandLine().UseHost(
                    _ => Host.CreateDefaultBuilder(args)
                        .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Information))
                        .ConfigureServices((_, services) => services.AddSearchConsole()))
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
            indexCommand.Handler = CommandHandler.Create<string, IHost, CancellationToken>(IndexDataIntoElastic);
            return indexCommand;
        }
        
        private static Command CreateSearchCommand()
        {
            Argument<string> typeArgument = new("type", "What types of documents you want to index");
            typeArgument.Suggestions.Add("entries");
            typeArgument.Suggestions.Add("leagues");

            Argument<string> termArgument = new("term", "Search term");
            termArgument.Suggestions.Add("Magnus Carlsen");
            
            var searchCommand = new Command("search", "Search data in ElasticSearch")
            {
                typeArgument,
                termArgument
            };
            searchCommand.Handler = CommandHandler.Create<string, string, IHost>(SearchDataInElastic);
            return searchCommand;
        }
        
        private static async Task IndexDataIntoElastic(string type, IHost host, CancellationToken token)
        {
            while (true)
            {
                if(token.IsCancellationRequested)
                {
                    return;
                }
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
            };
        }

        private static async Task SearchDataInElastic(string type, string term, IHost host)
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
                    RenderEntriesTable(entryResult);
                    break;
                case "leagues":
                    var leagueResult = await searchClient.SearchForLeague(term, 100, metaData, "no");
                    RenderLeaguesTable(leagueResult);
                    break;
            }
        }

        private static void RenderEntriesTable(SearchResult<EntryItem> entryResult)
        {
            var entryTable = new Table();
            entryTable.Border(TableBorder.Rounded);
            entryTable.Collapse();
            entryTable.AddColumn(new TableColumn("Team").Centered());
            entryTable.AddColumn(new TableColumn("Name").Centered());
            entryTable.AddColumn(new TableColumn("Id").Centered());
            entryTable.AddColumn(new TableColumn("Verified type").Centered());

            foreach (var entry in entryResult.ExposedHits)
            {
                entryTable.AddRow(entry.TeamName, entry.RealName, entry.Id.ToString(),
                    entry.VerifiedType.ToString() ?? string.Empty);
            }

            var rule = new Rule($"[green]Top {entryResult.Count} hits[/]");
            rule.LeftAligned();
            AnsiConsole.Render(rule);
            AnsiConsole.Render(entryTable);
            AnsiConsole.Render(rule);
        }

        private static void RenderLeaguesTable(SearchResult<LeagueItem> leagueResult)
        {
            var leagueTable = new Table();
            leagueTable.Border(TableBorder.Rounded);
            leagueTable.Collapse();
            leagueTable.AddColumn(new TableColumn("Name").Centered());
            leagueTable.AddColumn(new TableColumn("Id").Centered());
            leagueTable.AddColumn(new TableColumn("Admin entry").Centered());

            foreach (var entry in leagueResult.ExposedHits)
            {
                leagueTable.AddRow(entry.Name, entry.Id.ToString(), entry.AdminEntry.ToString());
            }

            var rule = new Rule($"[green]Top {leagueResult.Count} hits[/]");
            rule.LeftAligned();
            AnsiConsole.Render(rule);
            AnsiConsole.Render(leagueTable);
            AnsiConsole.Render(rule);
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSearchConsole(this IServiceCollection services)
        {
            var options = new Options();
            var conn = new ConnectionSettings(new Uri(options.Value.IndexUri));
            if (!string.IsNullOrEmpty(options.Value.Username) && !string.IsNullOrEmpty(options.Value.Password))
            {
                conn.BasicAuthentication(options.Value.Username, options.Value.Password);
            }

            var esClient = new ElasticClient(conn);
            services.AddSingleton<IElasticClient>(esClient);
            services.AddSingleton<IIndexingClient, IndexingClient>();
            services.AddSingleton<IOptions<SearchOptions>>(options);
            services.AddSingleton<IQueryAnalyticsIndexingService, QueryAnalyticsIndexingService>();
            services.AddSingleton<ILeagueClient, LeagueClient>(_ => new LeagueClient(CreateHttpClient()));
            services.AddHttpClient<IEntryClient, EntryClient>(_ => new EntryClient(CreateHttpClient()));
            services.AddSingleton<IIndexBookmarkProvider, SimpleLeagueIndexBookmarkProvider>();
            services.AddSingleton<IIndexProvider<EntryItem>, EntryIndexProvider>();
            services.AddSingleton<IIndexProvider<LeagueItem>, LeagueIndexProvider>();
            services.AddSingleton<IIndexingService, IndexingService>();
            services.AddSingleton<ISearchService, SearchService>();
            return services;
        }

        private static HttpClient CreateHttpClient()
        {
            var httpMessageHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };
            var c = new HttpClient(httpMessageHandler)
                {
                    BaseAddress = new Uri($"https://fantasy.premierleague.com")
                };
                c.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                c.DefaultRequestHeaders.Add("User-Agent", "Lol");
                return c;
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

