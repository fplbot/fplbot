using System.Net;
using Microsoft.Extensions.Options;

namespace FplBot.WebApi.Infrastructure;

public class BlockedIpMiddleware(RequestDelegate next, IOptionsMonitor<BlockedIpOptions> options, ILogger<BlockedIpMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var opts = options.CurrentValue;
        var blockedIps = opts.BlockedIpList;
        if (blockedIps.Length > 0 && IsProtectedPath(context.Request.Path, opts.ProtectedPaths))
        {
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp != null && IsBlocked(remoteIp, blockedIps))
            {
                logger.LogWarning("Blocked request from {Ip} to {Path}", remoteIp, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await next(context);
    }

    private static bool IsProtectedPath(PathString path, string[] protectedPaths) =>
        protectedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    private static bool IsBlocked(IPAddress remoteIp, string[] blockedIps)
    {
        // Normalize IPv4-mapped IPv6 addresses (e.g. ::ffff:1.2.3.4) before comparing
        var normalizedRemote = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;
        return blockedIps.Any(ip =>
            IPAddress.TryParse(ip, out var blocked) && normalizedRemote.Equals(blocked));
    }
}
