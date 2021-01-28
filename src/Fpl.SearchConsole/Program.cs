using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
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
                        .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Information))
                        .ConfigureServices((_, services) => services.AddSearchConsole()))
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }


        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand();
            root.AddCommand(CommandFactory.CreateIndexCommand());
            root.AddCommand(CommandFactory.CreateSearchCommand());
            return new CommandLineBuilder(root);
        }
    }
}

