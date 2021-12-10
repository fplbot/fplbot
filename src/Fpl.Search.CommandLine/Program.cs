using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Fpl.Search.Data;
using Fpl.SearchConsole;
using Fpl.SearchConsole.Commands.Definitions;
using NServiceBus;
using Serilog;
using StackExchange.Redis;

var root = new RootCommand();
root.AddCommand(IndexCommand.Create());
root.AddCommand(SearchCommand.Create());
var builder = new CommandLineBuilder(root);
builder.UseHost(_ => ConfigureHost(args))
        .UseDefaults()
        .Build()
        .InvokeAsync(args);

IHostBuilder ConfigureHost(string[] strings)
{
    return Host.CreateDefaultBuilder(strings)
        .UseSerilog((_, conf) => conf.WriteTo.Console())
        .UseNServiceBus(ctx =>
        {
            var endpointConfiguration = new EndpointConfiguration($"Fpl.Search.CommandLine.{ctx.HostingEnvironment.EnvironmentName}");
            endpointConfiguration.UseTransport<LearningTransport>();
            return endpointConfiguration;
        })
        .ConfigureServices((ctx, services) =>
        {
            var opts = new SearchRedisOptions()
            {
                REDIS_URL = ctx.Configuration["REDIS_URL"]
            };
            var options = new ConfigurationOptions
            {
                ClientName = opts.GetRedisUsername,
                Password = opts.GetRedisPassword,
                EndPoints = {opts.GetRedisServerHostAndPort}
            };
            var conn =  ConnectionMultiplexer.Connect(options);
            services.AddSearchConsole(ctx.Configuration, conn);
        });
}


