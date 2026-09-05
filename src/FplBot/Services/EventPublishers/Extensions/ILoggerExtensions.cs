using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers.Extensions;

internal static class ILoggerExtensions
{
    // Re-using the same as the correlationId ASP.NET Middleware, but re-defining it here to avoid dependency & it has a private accessor anyways :/
    // https://github.com/ekmsystems/serilog-enrichers-correlation-id/blob/master/src/Serilog.Enrichers.CorrelationId/Enrichers/CorrelationIdEnricher.cs#L15
    private const string CorrelationId = "CorrelationId";
    private const string Context = "Context";

    public static IDisposable BeginCorrelationScope(this ILogger logger)
    {
        return logger.BeginScope(new Dictionary<string, object> {[CorrelationId] = Guid.NewGuid()});
    }

    public static IDisposable AddContext(this ILogger logger, string context)
    {
        return logger.BeginScope(new Dictionary<string, object> {[Context] = context});
    }

    public static IDisposable AddContext(this ILogger logger, params Tuple<string,string>[] keyvaluepair)
    {
        var dictionary = keyvaluepair.ToDictionary<Tuple<string, string>, string, object>(kvp => kvp.Item1, kvp => kvp.Item2);
        return logger.BeginScope(dictionary);
    }
}
