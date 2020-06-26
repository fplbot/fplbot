using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
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
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Filter.ByExcluding(c => c.Properties.Any(p => p.Value.ToString().Contains("LogicalHandler")))
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}][{Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}",theme: SystemConsoleTheme.None))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "1337";
                    webBuilder.UseUrls($"http://*:{port}");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
