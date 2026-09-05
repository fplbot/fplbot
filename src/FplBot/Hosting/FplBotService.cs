namespace FplBot.Hosting;

public enum FplBotService
{
    WebApi,
    EventHandlers,
    EventPublishers,
    SearchIndexer,
}

public static class ArgsExtensions
{
    public static IReadOnlyList<FplBotService> ParseServices(this string[] args)
    {
        var idx = Array.IndexOf(args, "--services");
        if (idx < 0 || idx >= args.Length - 1)
            throw new InvalidOperationException(
                "No --services argument provided. Usage: --services \"WebApi\" or --services \"All\"");

        var value = args[idx + 1];
        if (value.Equals("All", StringComparison.OrdinalIgnoreCase))
            return Enum.GetValues<FplBotService>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Enum.Parse<FplBotService>(s, ignoreCase: true))
            .ToList();
    }
}
