using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Helpers;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Fpl.EventPublishers.States;

internal class NearDeadLineMonitor
{
    private readonly IGlobalSettingsClient _globalSettingsClient;
    private readonly DateTimeUtils _dateTimeUtils;
    private readonly IMessageSession _session;
    private readonly ILogger<NearDeadLineMonitor> _logger;

    public NearDeadLineMonitor(IGlobalSettingsClient globalSettingsClient, DateTimeUtils dateTimeUtils, IMessageSession session, ILogger<NearDeadLineMonitor> logger)
    {
        _globalSettingsClient = globalSettingsClient;
        _dateTimeUtils = dateTimeUtils;
        _session = session;
        _logger = logger;
    }

    public async Task EveryMinuteTick()
    {
        GlobalSettings globalSettings;
        try
        {
            globalSettings = await _globalSettingsClient.GetGlobalSettings();
        }
        catch (HttpRequestException hre) when (LogError(hre))
        {
            return;
        }
        var gweeks = globalSettings.Gameweeks;

        var next = gweeks.FirstOrDefault(gw => gw.IsNext);

        if (next != null)
        {
            if (_dateTimeUtils.IsWithinMinutesToDate(60, next.Deadline))
                await _session.Publish(new OneHourToDeadline(new GameweekNearingDeadline(next.Id, next.Name,next.Deadline)));

            if (_dateTimeUtils.IsWithinMinutesToDate(24*60, next.Deadline))
                await _session.Publish(new TwentyFourHoursToDeadline(new GameweekNearingDeadline(next.Id, next.Name,next.Deadline)));
        }
        else
        {
            _logger.LogInformation($"No next gameweek");
        }
    }

    private bool LogError(HttpRequestException hre)
    {
        _logger.LogWarning("Game is updating ({StatusCode})", hre.StatusCode);
        return hre.StatusCode == HttpStatusCode.ServiceUnavailable;
    }
}
