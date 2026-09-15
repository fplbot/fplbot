using System.Text.Json;
using FplBot.Hosting;

namespace FplBot.Data.Discord;

public static class DiscordChannelIndexMigration
{
    public static async Task Backup(string redisUrl, string outputPath)
    {
        var redis = FplBotApplication.ConnectToRedis(redisUrl);
        var db = redis.GetDatabase();
        var server = redis.GetServers().Single();

        var guildKeys = server.Keys(pattern: "Guild-*").ToList();
        var channelKeys = server.Keys(pattern: "GuildSubs-*-Channel-*").ToList();

        var dump = new Dictionary<string, Dictionary<string, string>>();
        foreach (var key in guildKeys.Concat(channelKeys))
        {
            var hash = await db.HashGetAllAsync(key);
            dump[key.ToString()] = hash.ToDictionary(h => h.Name.ToString(), h => h.Value.ToString());
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Backed up {guildKeys.Count} guild(s) and {channelKeys.Count} channel subscription(s) to {outputPath}");
    }

    public static async Task Backfill(string redisUrl)
    {
        var redis = FplBotApplication.ConnectToRedis(redisUrl);
        var db = redis.GetDatabase();
        var server = redis.GetServers().Single();

        var guildsIndexed = 0;
        foreach (var key in server.Keys(pattern: "Guild-*"))
        {
            var guildId = await db.HashGetAsync(key, "guildid");
            if (!guildId.HasValue) continue;
            await db.SetAddAsync("GuildIndex", guildId);
            guildsIndexed++;
        }

        var channelsIndexed = 0;
        foreach (var key in server.Keys(pattern: "GuildSubs-*-Channel-*"))
        {
            var fields = await db.HashGetAsync(key, ["guildid", "channelid"]);
            if (!fields[0].HasValue || !fields[1].HasValue) continue;
            await db.SetAddAsync($"GuildChannelSubIndex-{fields[0]}", fields[1]);
            channelsIndexed++;
        }

        Console.WriteLine($"Backfilled GuildIndex ({guildsIndexed} guild(s)) and GuildChannelSubIndex-* ({channelsIndexed} channel subscription(s))");
    }
}
