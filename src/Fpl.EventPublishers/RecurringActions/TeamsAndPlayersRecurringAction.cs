using CronBackgroundServices;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Fpl.EventPublishers.RecurringActions;

public class TeamsAndPlayersRecurringAction : IRecurringAction
{
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly IMessageSession _session;
    private readonly ILogger<TeamsAndPlayersRecurringAction> _logger;
    private ICollection<Player> _players;

    public TeamsAndPlayersRecurringAction(IGlobalSettingsClient settingsClient, IMessageSession session, ILogger<TeamsAndPlayersRecurringAction> logger)
    {
        _settingsClient = settingsClient;
        _session = session;
        _logger = logger;
        _players = new List<Player>();
    }

    public async Task Process(CancellationToken stoppingToken)
    {
        using var scope = _logger.AddContext("PlayersAndTeamState");
        var settings = await _settingsClient.GetGlobalSettings();
        if (_players == null || !_players.Any())
        {
            _logger.LogInformation($"Init state");
            _players = settings.Players;
            return;
        }

        _logger.LogInformation($"Refreshing");

        var globalSettings = await _settingsClient.GetGlobalSettings();
        var after = globalSettings.Players;
        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, _players, globalSettings.Teams);
        var injuryUpdates = PlayerChangesEventsExtractor.GetInjuryUpdates(after, _players, globalSettings.Teams);
        var newPlayers = PlayerChangesEventsExtractor.GetNewPlayers(after, _players, globalSettings.Teams);

        _players = after;

        if (priceChanges.Any())
            await _session.Publish(new PlayersPriceChanged(priceChanges.ToList()));

        if (injuryUpdates.Any())
            await _session.Publish(new InjuryUpdateOccured(injuryUpdates));

        if (newPlayers.Any())
            await _session.Publish(new NewPlayersRegistered(newPlayers.ToList()));
    }

    public string Cron => CronPatterns.EveryTwentySeconds;
}
