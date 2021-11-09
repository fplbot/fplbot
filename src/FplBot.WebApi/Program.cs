using FplBot.WebApi;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);
var startup = new Startup(builder.Configuration, builder.Environment); //TODO: Move from Startup
startup.ConfigureServices(builder.Services);
builder.Host.UseMessaging();
builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(hostingContext.Configuration)
        .Enrich.WithCorrelationId()
        .Enrich.WithCorrelationIdHeader()
        .WriteTo.Console(
            outputTemplate:
            "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}",
            theme: ConsoleTheme.None)
);

var app = builder.Build();
startup.Configure(app, app.Environment);
app.Run($"http://*:{Environment.GetEnvironmentVariable("PORT") ?? "1337"}");
