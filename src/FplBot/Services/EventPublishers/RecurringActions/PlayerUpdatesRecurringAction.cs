using System.Net;
using CronBackgroundServices;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.RecurringActions;

public class PlayerUpdatesRecurringAction(
    IGlobalSettingsClient settingsClient,
    IServiceScopeFactory scopeFactory,
    ILogger<PlayerUpdatesRecurringAction> logger)
    : IRecurringAction
{
    private ICollection<Player> _players = new List<Player>();

    public async Task Process(CancellationToken stoppingToken)
    {
        using var scope = logger.BeginCorrelationScope();
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
        var settings = await settingsClient.GetGlobalSettings();
        if (_players == null || !_players.Any())
        {
            logger.LogInformation($"Init state");
            _players = settings?.Players ?? new List<Fpl.Client.Models.Player>();
            return;
        }

        logger.LogInformation($"Refreshing");

        var globalSettings = await settingsClient.GetGlobalSettings();
        var after = globalSettings?.Players ?? new List<Fpl.Client.Models.Player>();
        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, _players, globalSettings?.Teams ?? new List<Fpl.Client.Models.Team>());
        var injuryUpdates = PlayerChangesEventsExtractor.GetInjuryUpdates(after, _players, globalSettings?.Teams ?? new List<Fpl.Client.Models.Team>());
        var newPlayers = PlayerChangesEventsExtractor.GetNewPlayers(after, _players, globalSettings?.Teams ?? new List<Fpl.Client.Models.Team>());
        var transfers = PlayerChangesEventsExtractor.GetInternalPLTransfers(after, _players, globalSettings?.Teams ?? new List<Fpl.Client.Models.Team>());

        _players = after;

        if (priceChanges.Any() || injuryUpdates.Any() || newPlayers.Any() || transfers.Any())
        {
            using var scope = scopeFactory.CreateScope();
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            if (priceChanges.Any())
                await publish.Publish(new PlayersPriceChanged(priceChanges.ToList()));

            if (injuryUpdates.Any())
                await publish.Publish(new InjuryUpdateOccured(injuryUpdates));

            if (newPlayers.Any())
                await publish.Publish(new NewPlayersRegistered(newPlayers.ToList()));

            if (transfers.Any())
                await publish.Publish(new PremiershipPlayerTransferred(transfers.ToList()));
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

    public string Cron => CronPatterns.EveryOtherMinuteAt40SecondsSharp;
}
