namespace FplBot.Hosting;

public static class FplBotApplication
{
    public static async Task RunAsync(string[] args, IReadOnlyList<FplBotService> services)
    {
        Console.WriteLine($"Starting FplBot with services: {string.Join(", ", services)}");

        // TODO: wire up service modules as projects are migrated in
        // For now, placeholder so the project builds and runs
        await Task.CompletedTask;
        throw new NotImplementedException(
            $"Service modules not yet wired up. Requested: {string.Join(", ", services)}");
    }
}
