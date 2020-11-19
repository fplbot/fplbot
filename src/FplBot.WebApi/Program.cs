using System;
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
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostingContext, loggerConfiguration) =>
                    loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.WithCorrelationId()
                    .Enrich.WithCorrelationIdHeader()
                    .WriteTo.Conditional(e => hostingContext.HostingEnvironment.IsDevelopment(), dev => dev.Console(outputTemplate: "{Level:u3} {Message:lj}{NewLine}{Exception}", theme: ConsoleTheme.None))
                    .WriteTo.Conditional(e => !hostingContext.HostingEnvironment.IsDevelopment(), testAndProd => testAndProd.Console(new RenderedCompactJsonFormatter()))
                )
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "1337";
                    webBuilder.UseUrls($"http://*:{port}");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
