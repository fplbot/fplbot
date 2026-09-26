using System.Diagnostics;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace FplBot.WebApi.Mcp;

// fplbot's own OpenTelemetry pipeline (FplBotApplication.ConfigureOpenTelemetry) only runs in
// local dev - nothing is exported anywhere in Production, so the MCP SDK's built-in
// Experimental.ModelContextProtocol metrics never leave the process there either. Structured
// logs are what this app already relies on for Production observability (Heroku log tailing),
// so MCP usage is logged the same way instead of wired into a metrics backend.
//
// No "disconnect" counterpart: this server runs in the SDK's default stateless session mode
// (WithHttpTransport() with no options), where every request is its own independent "session"
// and the server never issues an Mcp-Session-Id - confirmed empirically (McpEndpointsTests
// traffic against the real McpClient never sends a session id or a DELETE on dispose, only
// plain POSTs). There is no persistent connection for a client to close, so no event exists to
// log one.
public static class McpUsageLogging
{
    public static IMcpServerBuilder WithUsageLogging(this IMcpServerBuilder builder) => builder
        .WithMessageFilters(messageFilters =>
        {
            // "initialize" is the client's handshake that opens an MCP conversation - the
            // closest thing to a "connect" event. It never reaches AddCallToolFilter below,
            // which only fires for tools/call.
            messageFilters.AddIncomingFilter(next => async (context, cancellationToken) =>
            {
                if (context.JsonRpcMessage is JsonRpcRequest { Method: "initialize" })
                {
                    Logger(context.Services)?.LogInformation("MCP connect from {RemoteIp}", RemoteIp(context.Services));
                }

                await next(context, cancellationToken);
            });
        })
        .WithRequestFilters(requestFilters =>
        {
            requestFilters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await next(context, cancellationToken);
                stopwatch.Stop();
                Logger(context.Services)?.LogInformation(
                    "MCP tool call {Tool} from {RemoteIp} - {Outcome} in {ElapsedMs}ms",
                    context.Params.Name, RemoteIp(context.Services), result.IsError == true ? "error" : "ok", stopwatch.ElapsedMilliseconds);
                return result;
            });
        });

    private static ILogger<Program>? Logger(IServiceProvider? services) => services?.GetService<ILogger<Program>>();

    private static string? RemoteIp(IServiceProvider? services) =>
        services?.GetService<IHttpContextAccessor>()?.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
