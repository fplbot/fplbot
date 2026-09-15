using FplBot.Data.Discord;
using FplBot.Hosting;

if (args.Length > 0 && args[0] == "--backup-discord-channel-index")
{
    var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL")
                   ?? throw new InvalidOperationException("Set REDIS_URL before running this command.");

    await DiscordChannelIndexMigration.Backup(redisUrl, args[1]);
    return;
}

var services = args.ParseServices();
await FplBotApplication.RunAsync(args, services);
