using System.Reflection;

namespace FplBot.WebApi.Slack;

internal record DebugInfo(string MajorMinorPatch, string Informational, string Sha);
internal static class MetaService
{
    public static DebugInfo DebugInfo()
    {
        Assembly entryAssembly = Assembly.GetEntryAssembly();
        Version version = entryAssembly?.GetName()?.Version;
        string majorMinorPatch = $"{version?.Major}.{version?.Minor}.{version?.Build}";
        string informationalVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var sha = informationalVersion?.Split(".").Last();
        return new DebugInfo(majorMinorPatch,informationalVersion, sha) ;
    }
}
