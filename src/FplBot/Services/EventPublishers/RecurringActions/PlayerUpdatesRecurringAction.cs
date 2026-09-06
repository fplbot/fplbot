using System.Net;
using CronBackgroundServices;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers.RecurringActions;

public class PlayerUpdatesRecurringAction : IRecurringAction
{
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PlayerUpdatesRecurringAction> _logger;
    private ICollection<Player> _players;

    public PlayerUpdatesRecurringAction(IGlobalSettingsClient settingsClient, IPublishEndpoint publishEndpoint, ILogger<PlayerUpdatesRecurringAction> logger)
    {
        _settingsClient = settingsClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _players = new List<Player>();
    }

    public async Task Process(CancellationToken stoppingToken)
    {
        using var scope = _logger.BeginCorrelationScope();
        try
        {
            await PublishIfChanges();
        }
        catch (Exception e) when (LogError(e))
        {
        }
    }

    private async Task PublishIfChanges()
    {
        var settings = await _settingsClient.GetGlobalSettings();
        if (_players == null || !_players.Any())
        {
            _logger.LogInformation($"Init state");
            _players = settings?.Players ?? new List<Fpl.Client.Models.Player>();
            return;
        }

        _logger.LogInformation($"Refreshing");

        var globalSettings = await _settingsClient.GetGlobalSettings();
        var after = globalSettings?.Players ?? new List<Fpl.Client.Models.Player>();
        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, _players, globalSettings?.Teams ?? new List<Fpl.Client.Models.Team>());
        var injuryUpdates = PlayerChangesEventsExtractor.GetInjuryUpdates(after, _players, globalSettings?.Teams ?? new List<Fpl.Client.Models.Team>());
        var newPlayers = PlayerChangesEventsExtractor.GetNewPlayers(after, _players, globalSettings?.Teams ?? new List<Fpl.Client.Models.Team>());
        var transfers = PlayerChangesEventsExtractor.GetInternalPLTransfers(after, _players, globalSettings?.Teams ?? new List<Fpl.Client.Models.Team>());

        _players = after;

        if (priceChanges.Any())
            await _publishEndpoint.Publish(new PlayersPriceChanged(priceChanges.ToList()));

        if (injuryUpdates.Any())
            await _publishEndpoint.Publish(new InjuryUpdateOccured(injuryUpdates));

        if (newPlayers.Any())
            await _publishEndpoint.Publish(new NewPlayersRegistered(newPlayers.ToList()));

        if (transfers.Any())
            await _publishEndpoint.Publish(new PremiershipPlayerTransferred(transfers.ToList()));
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

    public string Cron => CronPatterns.EveryOtherMinuteAt40SecondsSharp;
}
