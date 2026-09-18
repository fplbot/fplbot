using System.Diagnostics;

namespace FplBot.Hosting;

public static class FplBotDiagnostics
{
    public const string ActivitySourceName = "FplBot";

    public const string TeamIdTag = "fplbot.team_id";
    public const string ChannelIdTag = "fplbot.channel_id";

    private static readonly Dictionary<FplBotService, ActivitySource> Sources =
        Enum.GetValues<FplBotService>().ToDictionary(s => s, s => new ActivitySource(SourceNameFor(s)));

    public static string SourceNameFor(FplBotService service) => $"{ActivitySourceName}.{service}";

    public static ActivitySource For(FplBotService service) => Sources[service];
}
