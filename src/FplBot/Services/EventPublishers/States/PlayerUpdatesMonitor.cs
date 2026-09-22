using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace Fpl.EventPublishers.States;

public class PlayerUpdatesMonitor(
    IGlobalSettingsClient settingsClient,
    IServiceScopeFactory scopeFactory,
    ILogger<PlayerUpdatesMonitor> logger)
{
    private ICollection<Player> _players = [];
    private readonly HashSet<int> _notifiedLikelyToChangePriceIds = [];

    public async Task Tick(CancellationToken stoppingToken)
    {
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
            logger.LogInformation("Init state");
            _players = settings?.Players ?? [];

            // Seed the dedup set from the baseline snapshot too, or a service restart (a daily
            // occurrence on Heroku) would treat every player already very likely to change price
            // as a fresh transition and announce all of them at once on the very next poll.
            foreach (var player in _players.Where(p => p.IsVeryLikelyToChangePrice()))
            {
                _notifiedLikelyToChangePriceIds.Add(player.Id);
            }

            return;
        }

        logger.LogInformation("Refreshing");

        var globalSettings = await settingsClient.GetGlobalSettings();
        var after = globalSettings?.Players ?? [];
        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, _players, globalSettings?.Teams ?? []).ToList();
        var injuryUpdates = PlayerChangesEventsExtractor.GetInjuryUpdates(after, _players, globalSettings?.Teams ?? []);
        var newPlayers = PlayerChangesEventsExtractor.GetNewPlayers(after, _players, globalSettings?.Teams ?? []);
        var transfers = PlayerChangesEventsExtractor.GetInternalPLTransfers(after, _players, globalSettings?.Teams ?? []);
        var likelyPriceChanges = GetUnnotifiedLikelyPriceChanges(after, priceChanges, globalSettings?.Teams ?? []);

        _players = after;

        if (priceChanges.Any() || injuryUpdates.Any() || newPlayers.Any() || transfers.Any() || likelyPriceChanges.Any())
        {
            using var scope = scopeFactory.CreateScope();
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            if (priceChanges.Any())
                await publish.Publish(new PlayersPriceChanged([.. priceChanges]));

            if (injuryUpdates.Any())
                await publish.Publish(new InjuryUpdateOccured(injuryUpdates));

            if (newPlayers.Any())
                await publish.Publish(new NewPlayersRegistered([.. newPlayers]));

            if (transfers.Any())
                await publish.Publish(new PremiershipPlayerTransferred([.. transfers]));

            if (likelyPriceChanges.Any())
                await publish.Publish(new PlayersLikelyToChangePrice([.. likelyPriceChanges]));
        }
    }

    // FPL's "likelihood" can oscillate around the ±5 boundary between polls, so a raw
    // threshold check would re-notify every couple of minutes. Fire once per streak of
    // being very likely, and only re-arm once the player's momentum resets to neutral
    // or their price actually changes.
    private List<PlayerLikelyPriceChange> GetUnnotifiedLikelyPriceChanges(ICollection<Player> after, IEnumerable<PlayerWithPriceChange> actualPriceChanges, ICollection<Team> teams)
    {
        foreach (var changed in actualPriceChanges)
        {
            _notifiedLikelyToChangePriceIds.Remove(changed.PlayerId);
        }

        foreach (var player in after.Where(p => p.NextPriceChangeLikelihood == 0))
        {
            _notifiedLikelyToChangePriceIds.Remove(player.Id);
        }

        var candidates = PlayerChangesEventsExtractor.GetLikelyPriceChanges(after, teams);
        return [.. candidates.Where(c => _notifiedLikelyToChangePriceIds.Add(c.PlayerId))];
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
