using System.Reflection;

namespace FplBot.WebApi.Slack;

public record DebugInfo(string MajorMinorPatch, string Informational, string Sha);
public static class MetaService
{
    public static DebugInfo DebugInfo()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        string? informationalVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var parts = informationalVersion?.Split('+');
        var majorMinorPatch = parts?[0] ?? "";
        var sha = parts?.Length > 1 ? parts[1] : "";
        return new DebugInfo(majorMinorPatch, informationalVersion ?? "", sha);
    }
}
