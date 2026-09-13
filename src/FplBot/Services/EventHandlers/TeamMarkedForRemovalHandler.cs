using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;

namespace FplBot.EventHandlers;

// Best-effort: tells Slack to revoke the app, then finalizes the delete regardless of whether
// that call succeeded (the token may already be dead - that's expected, not an error). Stays
// silent - admin-initiated removal doesn't post to #fplbot-notifications.
public class TeamMarkedForRemovalHandler(
    ISlackTeamRepository repository,
    ISlackClientBuilder slackClientBuilder,
    IOptions<OAuthOptions> slackAppOptions,
    ILogger<TeamMarkedForRemovalHandler> logger) : IConsumer<TeamMarkedForRemoval>
{
    public async Task Consume(ConsumeContext<TeamMarkedForRemoval> context)
    {
        var installation = await repository.FindInstallationByTeamId(context.Message.TeamId);

        if (installation is null)
        {
            logger.LogWarning("TeamMarkedForRemoval for {TeamId} but no such team found", context.Message.TeamId);
            return;
        }

        if (!installation.PendingRemoval)
        {
            logger.LogWarning("TeamMarkedForRemoval for {TeamId} but team not marked for removal", context.Message.TeamId);
            return;
        }

        if(installation.Token is not { Length: > 0 })
        {
            logger.LogWarning("TeamMarkedForRemoval for {TeamId} but team has no access token. Just deleting without telling Slack.", context.Message.TeamId);
            await repository.DeleteByTeamId(context.Message.TeamId);
            return;
        }

        var slackClient = slackClientBuilder.Build(token: installation.Token);
        try
        {
            var response = await slackClient.AppsUninstall(slackAppOptions.Value.CLIENT_ID, slackAppOptions.Value.CLIENT_SECRET);
            if (!response.Ok)
            {
                logger.LogWarning("Slack apps.uninstall for {TeamId} returned {Error}", context.Message.TeamId, response.Error);
            }
            else
            {
                logger.LogInformation("Slack apps.uninstall for {TeamId} succeeded", context.Message.TeamId);
            }
        }
        catch (WellKnownSlackApiException e)
        {
            logger.LogInformation("Slack apps.uninstall for {TeamId} failed (likely already revoked): {Message}. Deleting.", context.Message.TeamId, e.Message);
        }

        await repository.DeleteByTeamId(context.Message.TeamId);
        logger.LogWarning("Team {TeamId} marked for removal has been deleted from the database", context.Message.TeamId);
    }
}
