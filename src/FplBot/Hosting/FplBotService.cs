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
        if (idx < 0)
            return Enum.GetValues<FplBotService>();

        if (idx >= args.Length - 1)
            throw new InvalidOperationException(
                "--services was provided without a value. Usage: --services \"WebApi\" or --services \"All\", or omit it to run all services.");

        var value = args[idx + 1];
        if (value.Equals("All", StringComparison.OrdinalIgnoreCase))
            return Enum.GetValues<FplBotService>();

        return
        [
            .. value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.Parse<FplBotService>(s, ignoreCase: true))
        ];
    }
}
