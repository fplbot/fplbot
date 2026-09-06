using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Helpers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.States;

internal class NearDeadLineMonitor
{
    private readonly IGlobalSettingsClient _globalSettingsClient;
    private readonly DateTimeUtils _dateTimeUtils;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NearDeadLineMonitor> _logger;

    public NearDeadLineMonitor(IGlobalSettingsClient globalSettingsClient, DateTimeUtils dateTimeUtils, IServiceScopeFactory scopeFactory, ILogger<NearDeadLineMonitor> logger)
    {
        _globalSettingsClient = globalSettingsClient;
        _dateTimeUtils = dateTimeUtils;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task EveryMinuteTick()
    {
        GlobalSettings? globalSettings;
        try
        {
            globalSettings = await _globalSettingsClient.GetGlobalSettings();
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
            using var scope = _scopeFactory.CreateScope();
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            if (_dateTimeUtils.IsWithinMinutesToDate(60, next.Deadline))
                await publish.Publish(new OneHourToDeadline(new GameweekNearingDeadline(next.Id, next.Name ?? "",next.Deadline)));

            if (_dateTimeUtils.IsWithinMinutesToDate(24*60, next.Deadline))
                await publish.Publish(new TwentyFourHoursToDeadline(new GameweekNearingDeadline(next.Id, next.Name ?? "",next.Deadline)));
        }
        else
        {
            _logger.LogInformation($"No next gameweek");
        }
    }

    private bool LogError(Exception e)
    {
        if (e is HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable })
        {
            _logger.LogWarning("Game is updating");
        }
        else
        {
            _logger.LogError(e, e.Message);
        }

        return true;
    }
}
