using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Fpl.SearchConsole.Commands.Definitions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Serilog;

namespace Fpl.SearchConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await BuildCommandLine()
                .UseHost(_ => Host.CreateDefaultBuilder(args)
                                    .UseSerilog((_, conf) => conf.WriteTo.Console())
                                    .UseNServiceBus(ctx =>
                                    {
                                        var endpointConfiguration = new EndpointConfiguration($"Fpl.SearchConsole.{ctx.HostingEnvironment.EnvironmentName}");
                                        endpointConfiguration.UseTransport<LearningTransport>();
                                        return endpointConfiguration;
                                    })
                                    .ConfigureServices((_, services) => services.AddSearchConsole(_.Configuration)
                ))
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }


        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand();
            root.AddCommand(IndexCommand.Create());
            root.AddCommand(SearchCommand.Create());
            return new CommandLineBuilder(root);
        }
    }
}

