namespace FplBot.WebApi.Infrastructure;

public class BlockedIpOptions
{
    public string[] BlockedIps { get; set; } = [];
    public string[] ProtectedPaths { get; set; } = ["/search"];
}
