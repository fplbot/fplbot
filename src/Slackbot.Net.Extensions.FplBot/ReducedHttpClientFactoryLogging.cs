using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddReducedHttpClientFactoryLogging(this IServiceCollection services)
        {
            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
            services.AddSingleton<IHttpMessageHandlerBuilderFilter, ReducedLoggingHttpMessageHandlerBuilderFilter>();
            return services;
        }
    }
    
    internal class ReducedLoggingHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly ILoggerFactory _loggerFactory;

        public ReducedLoggingHttpMessageHandlerBuilderFilter(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            
            _loggerFactory = loggerFactory;
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            return (builder) =>
            {
                // Run other configuration first, we want to decorate.
                next(builder);

                string loggerName = !string.IsNullOrEmpty(builder.Name) ? builder.Name : "Default";


                ILogger innerLogger = _loggerFactory.CreateLogger($"System.Net.Http.HttpClient.{loggerName}.ClientHandler");


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
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(new EventId(101, "RequestEnd"), string.Format("{0} - {1} in {2}ms", request.RequestUri.ToString(), response.StatusCode, stopwatch.GetElapsedTime().TotalMilliseconds));

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
                // Start timestamp can't be zero in an initialized ValueStopwatch. It would have to be literally the first thing executed when the machine boots to be 0.
                // So it being 0 is a clear indication of default(ValueStopwatch)
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
}