using System.Collections.Concurrent;
using System.Text.Json;
using FplBot.Hosting;

namespace FplBot.Data.Discord;

public static class DiscordChannelIndexMigration
{
    private const int MaxConcurrentFetches = 64;

    public static async Task Backup(string redisUrl, string outputPath)
    {
        var redis = FplBotApplication.ConnectToRedis(redisUrl);
        var db = redis.GetDatabase();
        var server = redis.GetServers().Single();

        var guildKeys = server.Keys(pattern: "Guild-*").ToList();
        var channelKeys = server.Keys(pattern: "GuildSubs-*-Channel-*").ToList();

        var dump = new ConcurrentDictionary<string, Dictionary<string, string>>();
        await Parallel.ForEachAsync(guildKeys.Concat(channelKeys), new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentFetches }, async (key, _) =>
        {
            var hash = await db.HashGetAllAsync(key);
            dump[key.ToString()] = hash.ToDictionary(h => h.Name.ToString(), h => h.Value.ToString());
        });

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(dump.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value), new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Backed up {guildKeys.Count} guild(s) and {channelKeys.Count} channel subscription(s) to {outputPath}");
    }

    public static async Task Backfill(string redisUrl)
    {
        var redis = FplBotApplication.ConnectToRedis(redisUrl);
        var db = redis.GetDatabase();
        var server = redis.GetServers().Single();

        var guildKeys = server.Keys(pattern: "Guild-*").ToList();
        var channelKeys = server.Keys(pattern: "GuildSubs-*-Channel-*").ToList();

        var guildsIndexed = 0;
        await Parallel.ForEachAsync(guildKeys, new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentFetches }, async (key, _) =>
        {
            var guildId = await db.HashGetAsync(key, "guildid");
            if (!guildId.HasValue) return;
            await db.SetAddAsync("GuildIndex", guildId);
            Interlocked.Increment(ref guildsIndexed);
        });

        var channelsIndexed = 0;
        await Parallel.ForEachAsync(channelKeys, new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentFetches }, async (key, _) =>
        {
            var fields = await db.HashGetAsync(key, ["guildid", "channelid"]);
            if (!fields[0].HasValue || !fields[1].HasValue) return;
            await db.SetAddAsync($"GuildChannelSubIndex-{fields[0]}", fields[1]);
            Interlocked.Increment(ref channelsIndexed);
        });

        Console.WriteLine($"Backfilled GuildIndex ({guildsIndexed} guild(s)) and GuildChannelSubIndex-* ({channelsIndexed} channel subscription(s))");
    }
}
