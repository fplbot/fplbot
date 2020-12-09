using System;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

namespace FplBot.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
            Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyVersionAttribute>()?.Version);
            Console.WriteLine(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseMessaging()
                .UseSerilog((hostingContext, loggerConfiguration) =>
                    loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.WithCorrelationId()
                    .Enrich.WithCorrelationIdHeader()
                    .WriteTo.Console(outputTemplate: "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}", theme: ConsoleTheme.None)
                )
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "1337";
                    webBuilder.UseUrls($"http://*:{port}");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
