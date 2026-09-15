using System.Diagnostics;

namespace FplBot.Hosting;

public static class FplBotDiagnostics
{
    public const string ActivitySourceName = "FplBot";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
