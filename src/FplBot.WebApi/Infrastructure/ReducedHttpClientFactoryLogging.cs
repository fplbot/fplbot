using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReducedHttpClientFactoryLogging(this IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, ReducedLoggingHttpMessageHandlerBuilderFilter>());
        return services;
    }
}
    
internal class ReducedLoggingHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly ILoggerFactory _loggerFactory;

    public ReducedLoggingHttpMessageHandlerBuilderFilter(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);

            var loggerName = !string.IsNullOrEmpty(builder.Name) ? builder.Name : "Default";
            var innerLogger = _loggerFactory.CreateLogger($"System.Net.Http.HttpClient.{loggerName}.ClientHandler");
            var toRemove = builder.AdditionalHandlers.Where(h => (h is LoggingHttpMessageHandler) || h is LoggingScopeHttpMessageHandler).Select(h => h).ToList();
            foreach (var delegatingHandler in toRemove)
            {
                builder.AdditionalHandlers.Remove(delegatingHandler);
            }
            builder.AdditionalHandlers.Add(new MinimalLoggingHandler(innerLogger));
        };
    }
}
    
public class MinimalLoggingHandler : DelegatingHandler
{
    private readonly ILogger _logger;

    public MinimalLoggingHandler(ILogger logger) 
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var stopwatch = ValueStopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(new EventId(101, "RequestEnd"), $"{request.Method} {request.RequestUri} - {response.StatusCode} in {stopwatch.GetElapsedTime().TotalMilliseconds}ms");

        return response;
    }
        
    internal struct ValueStopwatch
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private long _startTimestamp;

        public bool IsActive => _startTimestamp != 0;

        private ValueStopwatch(long startTimestamp)
        {
            _startTimestamp = startTimestamp;
        }

        public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

        public TimeSpan GetElapsedTime()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
            }

            long end = Stopwatch.GetTimestamp();
            long timestampDelta = end - _startTimestamp;
            long ticks = (long)(TimestampToTicks * timestampDelta);
            return new TimeSpan(ticks);
        }
    }
}
