namespace FplBot.WebApi.Infrastructure;

public class BlockedIpOptions
{
    // Comma-separated for easy Heroku config var management: "1.2.3.4,5.6.7.8"
    public string BlockedIps { get; set; } = "";

    public string[] BlockedIpList =>
        BlockedIps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public string[] ProtectedPaths { get; set; } = ["/search"];
}
