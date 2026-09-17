namespace FplBot.Hosting;

public static class HostEnvironmentExtensions
{
    public const string Integration = "Integration";

    // Integration runs on a developer machine exactly like Development — user secrets, https on
    // localhost, telemetry to the Aspire dashboard — but talks to the real Slack and Discord
    // APIs rather than the dev-logging stand-ins. Use IsLocal for machine conveniences and
    // IsDevelopment only where an outbound integration is being faked.
    public static bool IsLocal(this IHostEnvironment env) =>
        env.IsDevelopment() || env.IsEnvironment(Integration);
}
