using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Helpers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.States;

internal class NearDeadLineMonitor(
    IGlobalSettingsClient globalSettingsClient,
    DateTimeUtils dateTimeUtils,
    IServiceScopeFactory scopeFactory,
    ILogger<NearDeadLineMonitor> logger)
{
    public async Task EveryMinuteTick()
    {
        GlobalSettings? globalSettings;
        try
        {
            globalSettings = await globalSettingsClient.GetGlobalSettings();
        }
        catch (HttpRequestException hre) when (LogError(hre))
        {
            return;
        }

        if (globalSettings == null)
            return;

        var gweeks = globalSettings.Gameweeks;

        var next = gweeks.FirstOrDefault(gw => gw.IsNext);

        if (next != null)
        {
            using var scope = scopeFactory.CreateScope();
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            if (dateTimeUtils.IsWithinMinutesToDate(60, next.Deadline))
                await publish.Publish(new OneHourToDeadline(new GameweekNearingDeadline(next.Id, next.Name ?? "",next.Deadline)));

            if (dateTimeUtils.IsWithinMinutesToDate(24*60, next.Deadline))
                await publish.Publish(new TwentyFourHoursToDeadline(new GameweekNearingDeadline(next.Id, next.Name ?? "",next.Deadline)));
        }
        else
        {
            logger.LogInformation($"No next gameweek");
        }
    }

    private bool LogError(Exception e)
    {
        if (e is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable })
        {
            logger.LogWarning("Game is updating");
        }
        else
        {
            logger.LogError(e, e.Message);
        }

        return true;
    }
}
