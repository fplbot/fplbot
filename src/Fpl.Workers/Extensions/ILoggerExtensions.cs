using Microsoft.Extensions.Logging;

namespace Fpl.Workers.Extensions;

internal static class ILoggerExtensions
{
    // Re-using the same as the correlationId ASP.NET Middleware, but re-defining it here to avoid dependency & it has a private accessor anyways :/ 
    // https://github.com/ekmsystems/serilog-enrichers-correlation-id/blob/master/src/Serilog.Enrichers.CorrelationId/Enrichers/CorrelationIdEnricher.cs#L15
    private const string CorrelationId = "CorrelationId";

    public static IDisposable BeginCorrelationScope(this ILogger logger)
    {
        return logger.BeginScope(new Dictionary<string, object> {[CorrelationId] = Guid.NewGuid()});
    }
}