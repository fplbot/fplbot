using System.Diagnostics;
using System.Net.Security;
using Fpl.EventPublishers.RecurringActions;
using FplBot.Data;
using Discord.Net.Endpoints;
using FplBot.WebApi.Infrastructure;
using FplBot.Services.EventHandlers;
using FplBot.Services.EventPublishers;
using FplBot.Services.SearchIndexer;
using FplBot.Services.WebApi;
using MassTransit;
using MassTransit.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;

namespace FplBot.Hosting;

public static class FplBotApplication
{
    private static readonly IFplBotService[] AllServices =
    [
        new WebApiService(),
        new EventHandlersService(),
        new EventPublishersService(),
        new SearchIndexerService()
    ];

    public static async Task RunAsync(string[] args, IReadOnlyList<FplBotService> activeServices)
    {
        var selectedServices = AllServices.Where(s => activeServices.Contains(s.ServiceType)).ToList();
        var hostOwner = selectedServices.FirstOrDefault(s => s.ServiceType == FplBotService.WebApi) ?? selectedServices[0];
        await hostOwner.RunHostAsync(args, selectedServices);
    }

    // Telemetry only goes somewhere in local envs, where the Aspire dashboard is listening. Shared
    // so anything pointing an operator at a trace agrees with whether traces are being collected.
    public static bool IsTelemetryEnabled(IHostEnvironment env, IConfiguration config) =>
        env.IsLocal() && config.GetValue("OTEL_ENABLED", true);

    internal static void LogStartup(IHost host, List<IFplBotService> active)
    {
        host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(FplBotApplication))
            .LogInformation("Starting services: {ServiceName}", string.Join(",", active.Select(s => s.ServiceType)));
    }

    // Both host builders load user secrets in Development only. Integration is just as local, and
    // needs the same real Slack/Discord credentials, so add them there too.
    internal static void AddLocalUserSecrets(IConfigurationBuilder config, IHostEnvironment env)
    {
        if (env.IsEnvironment(HostEnvironmentExtensions.Integration))
            config.AddUserSecrets(typeof(FplBotApplication).Assembly, optional: true);
    }

    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration config,
        ConnectionMultiplexer redisConn,
        IHostEnvironment env,
        List<IFplBotService> active,
        Action<IBusRegistrationConfigurator> configureBus)
    {
        ConfigureCommon(services, config, redisConn);
        services.AddMassTransit(x =>
        {
            x.DisableUsageTelemetry();

            Dictionary<Type, FplBotService> consumerOwners = [];
            foreach (var svc in active)
            {
                var before = x.Count;
                svc.AddConsumers(x);
                foreach (var descriptor in x.Skip(before).Where(d => typeof(IConsumer).IsAssignableFrom(d.ServiceType)))
                    consumerOwners[descriptor.ServiceType] = svc.ServiceType;
            }

            x.AddConfigureEndpointsCallback((_, _, cfg) =>
                cfg.ConnectConsumerConfigurationObserver(new ConsumerActivityObserver(consumerOwners)));

            configureBus(x);
        });

        if (IsTelemetryEnabled(env, config))
        {
            ConfigureOpenTelemetry(services, config, active);
        }

        foreach (var svc in active)
            svc.Configure(services, config, redisConn, env);
    }

    // Shared with OpenTelemetry tracing/metrics resource attribution — every telemetry signal
    // reports under the same per-process service.name so the Aspire dashboard can tell the four
    // FplBot services apart even though they're all built from one assembly.
    public static string GetOtelServiceName(IEnumerable<IFplBotService> active) =>
        string.Join("+", active.Select(s => s.ServiceType));


    // The one place logging is turned on. A host that wants no logging - the test host - simply does
    // not call these, and nothing else has to know.
    public static void WireUpLogging(WebApplicationBuilder builder, List<IFplBotService> active) =>
        builder.Host.UseSerilog((ctx, lc) => ConfigureSerilog(ctx, lc, active));

    public static void UseLogging(WebApplication app) => app.UseSerilogRequestLogging();

    internal static void ConfigureSerilog(HostBuilderContext ctx, LoggerConfiguration lc, List<IFplBotService> active)
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console(
                outputTemplate: "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                theme: ConsoleTheme.None);

        if (ctx.HostingEnvironment.IsLocal())
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[Serilog SelfLog] {msg}"));
            lc.WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint = ctx.Configuration["OTLP_DASHBOARD_ENDPOINT"];
                o.Protocol = OtlpProtocol.Grpc;
                o.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = GetOtelServiceName(active) };
            });
        }
    }

    // Matches FplBot.AppHost/Program.cs's builder.AddServiceBusEmulator(..., port: 6000). The
    // emulator's own admin REST calls (create queue/topic/subscription) show up as unnamed
    // GET/PUT spans with no useful attributes — MassTransit's "Configure Topology" activity
    // already reports the same setup with a readable name, so the raw HTTP calls are just noise.
    private const int LocalServiceBusEmulatorPort = 6000;

    private static void ConfigureOpenTelemetry(IServiceCollection services, IConfiguration config, List<IFplBotService> fplServices)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(GetOtelServiceName(fplServices)))
            .WithTracing(tracing => tracing
                .SetSampler(new FplBotSampler(
                    // MassTransit's own "Configure Topology" spans are per-queue startup wiring, not
                    // application behavior — they flood the dashboard with dozens of near-identical,
                    // attribute-less traces on every dev restart.
                    dropped: ["Configure Topology"],
                    exportedOnlyWhenPublishing:
                    [
                        nameof(NearDeadlineRecurringAction),
                        nameof(PlayerUpdatesRecurringAction),
                        nameof(GameweekLifecycleRecurringAction),
                        nameof(GuildStatusChecker)
                    ]))
                .AddAspNetCoreInstrumentation(o => o.EnrichWithHttpResponse = (activity, response) =>
                {
                    // Webhook paths are app.Map() middleware, not routed endpoints, so routing
                    // resolves them to the SPA catch-all and names every one of them
                    // "POST {*path:nonfile}". Rename to the mounted path. Only these two — naming
                    // any unrouted request by its raw path would mint a span name per 404.
                    var request = response.HttpContext.Request;
                    if (WebAppExtensions.WebhookPaths.Contains(request.Path.Value))
                        activity.DisplayName = $"{request.Method} {request.Path.Value}";
                })
                .AddHttpClientInstrumentation(o =>
                {
                    o.FilterHttpRequestMessage = req => req.RequestUri?.Port != LocalServiceBusEmulatorPort;
                    // Outbound spans default to a bare "GET"/"POST" — indistinguishable from any other
                    // outbound call in a flat trace list. Name them by destination host so e.g. a push
                    // delivery to fcm.googleapis.com doesn't require opening the span to identify.
                    o.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        if (request.RequestUri is { } uri)
                            activity.DisplayName = $"{request.Method} {uri.Host}";
                    };
                })
                .AddSource(DiagnosticHeaders.DefaultListenerName)
                .AddSource(DiscordDiagnostics.ActivitySourceName)
                .AddSource([.. fplServices.Select(svc => FplBotDiagnostics.SourceNameFor(svc.ServiceType))])
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(config["OTLP_DASHBOARD_ENDPOINT"]!);
                    o.Protocol = OtlpExportProtocol.Grpc;
                }));
    }

    private sealed class FplBotSampler(string[] dropped, string[] exportedOnlyWhenPublishing) : Sampler
    {
        private static readonly SamplingResult Drop = new(SamplingDecision.Drop);
        private static readonly SamplingResult Export = new(SamplingDecision.RecordAndSample);
        private static readonly SamplingResult ExportOnlyIfPromoted = new(SamplingDecision.RecordOnly);

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            if (dropped.Contains(samplingParameters.Name))
                return Drop;

            if (exportedOnlyWhenPublishing.Contains(samplingParameters.Name))
                return ExportOnlyIfPromoted;

            var unpublishedTick = NearestUnpublishedTick();
            if (unpublishedTick is null)
                return Export;

            if (samplingParameters.Kind != ActivityKind.Producer)
                return Drop;

            Promote(unpublishedTick);
            return Export;
        }

        private Activity? NearestUnpublishedTick()
        {
            for (var ancestor = Activity.Current; ancestor is not null; ancestor = ancestor.Parent)
                if (!ancestor.Recorded && exportedOnlyWhenPublishing.Contains(ancestor.OperationName))
                    return ancestor;

            return null;
        }

        private static void Promote(Activity tick)
        {
            for (var ancestor = Activity.Current; ancestor is not null; ancestor = ancestor.Parent)
            {
                ancestor.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
                if (ancestor == tick)
                    return;
            }
        }
    }

    private static void ConfigureCommon(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redisConn)
    {
        services.AddSingleton<IConnectionMultiplexer>(redisConn);
        services.AddSingleton(redisConn);
        services.AddSingleton<IIdentityResolver, IdentityResolver>();
        services.AddStackExchangeRedisCache(o => o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redisConn));
        services.AddReducedHttpClientFactoryLogging();
        services.AddFplApiClient(config);
    }

    internal static void ConfigureAzureServiceBus(IBusRegistrationConfigurator cfg, IConfiguration config)
    {
        cfg.UsingAzureServiceBus((ctx, bus) =>
        {
            var connectionString = config["ASB_CONNECTIONSTRING"]
                                   ?? throw new InvalidOperationException(
                                       "Service bus connection string not configured. Set ConnectionStrings__servicebus or ASB_CONNECTIONSTRING.");
            bus.Host(connectionString);
            bus.UseServiceBusMessageScheduler();
            bus.DefaultMessageTimeToLive = TimeSpan.FromHours(2);

            // Entity-level TTL on the auto-provisioned _error/_skipped queues (MassTransit calls the
            // latter "DeadLetterSettings", but it's the per-consumer skip queue, not ASB's native DLQ
            // subqueue). This is separate from the DefaultMessageTimeToLive above, which only stamps a
            // per-message send-time TTL and never applies to a queue entity. Without this, the _error/
            // _skipped queues' own entity TTL defaults to MassTransit's ~366 days, so faulted/skipped
            // messages accumulate indefinitely regardless of the original message's TTL.
            bus.SendTopology.ConfigureErrorSettings = e => e.DefaultMessageTimeToLive = TimeSpan.FromDays(2);
            bus.SendTopology.ConfigureDeadLetterSettings = e => e.DefaultMessageTimeToLive = TimeSpan.FromDays(2);

            bus.ConfigureEndpoints(ctx);
        });
    }

    internal static ConnectionMultiplexer BuildRedisConnection(IConfiguration config)
    {
        var connectionString = config["REDIS_URL"];
        if (connectionString == null)
            throw new InvalidOperationException("Redis connection string not configured. Set REDIS_URL.");

        if (connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectionMultiplexer.Connect(ParseRedisUrl(connectionString));
        }

        return ConnectionMultiplexer.Connect(connectionString);
    }

    private static ConfigurationOptions ParseRedisUrl(string redisUrl)
    {
        var uri = new Uri(redisUrl);
        var userInfo = uri.UserInfo.Split(':');
        var host = uri.Host + ":" + uri.Port;
        var options = new ConfigurationOptions
        {
            Password = userInfo.Length > 1 ? userInfo[1] : null,
            EndPoints = { host },
            Ssl = redisUrl.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase),
            SslClientAuthenticationOptions = _ => new SslClientAuthenticationOptions
            {
                TargetHost = uri.Host,
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            }
        };
        if (!string.IsNullOrEmpty(userInfo[0]))
            options.User = userInfo[0];
        return options;
    }
}
