using Discord.Net.Endpoints.Hosting;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.Services.WebApi.Discord.Handlers.Reactors;

public class DiscordNetInstallationBridge(
    IGuildRepository repository,
    IPublishEndpoint publisher,
    ILogger<DiscordNetInstallationBridge> logger
) : IGuildInstallationHandler
{
    public async Task Install(Guild guild)
    {
        var existing = await repository.FindInstallationByTeamId(guild.Id);
        var installation = existing is not null
            ? Installation.Reinstall(guild.Id, guild.Name, existing.ChannelSubscriptions)
            : Installation.Install(guild.Id, guild.Name);
        await repository.Save(installation);
        await publisher.Publish(new AppInstalled(guild.Id, guild.Name, ChatPlatform.Discord));
    }

    public async Task Uninstall(string guildId)
    {
        var installation = await repository.FindInstallationByTeamId(guildId);
        if (installation is null)
        {
            logger.LogWarning(
                "Uninstall called for guildId {guildId} but no installation found. Bot was already deleted",
                guildId);
            return;
        }

        await repository.Delete(installation);
        await publisher.Publish(new AppUninstalled(installation.Id, installation.Name));
    }
}
