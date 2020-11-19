using System;
using System.Collections.Generic;


namespace Microsoft.Extensions.Logging
{
    internal static class ILoggerExtensions
    {
        public static IDisposable BeginCorrelationScope(this ILogger logger)
        {
            return logger.BeginScope(new Dictionary<string, object> {["CorrelationId"] = Guid.NewGuid()});
        }
    }
}