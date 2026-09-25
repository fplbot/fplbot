using Discord.Net.HttpClients;
using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class UninstallGuildHandler(
    IGuildRepository repository,
    IGuildMemberCountRepository guildMemberCountRepo,
    IDiscordClient discordClient,
    ILogger<UninstallGuildHandler> logger) : IConsumer<UninstallGuild>
{
    public async Task Consume(ConsumeContext<UninstallGuild> context)
    {
        var guildId = context.Message.GuildId;
        var installation = await repository.FindInstallationByTeamId(guildId);

        if (installation is not null)
        {
            try
            {
                await discordClient.GuildLeave(guildId);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Could not leave guild {GuildId}, uninstalling anyway", guildId);
            }

            await repository.Delete(installation);
            await context.Publish(new AppUninstalled(installation.ExternalId, installation.Name, context.Message.Reason));
        }

        // Runs even if the installation is already gone - e.g. a retry after a first attempt
        // deleted it but faulted before this step. Deleting an already-absent entry is a no-op.
        await guildMemberCountRepo.Delete(guildId);
    }
}
