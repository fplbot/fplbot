using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Helpers;
using FplBot.WebApi.Slack.Extensions;
using NServiceBus;

namespace FplBot.WebApi.Handlers.Commands;

public record SeedSelfishness(int EntryId) : ICommand;

public class SeedSelfishnessHandler : IHandleMessages<SeedSelfishness>
{
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly SelfOwnerShipCalculator _calculator;
    private readonly ILiveClient _liveClient;
    private readonly IVerifiedPLEntriesRepository _plRepo;
    private readonly ILogger<SeedSelfishnessHandler> _logger;

    public SeedSelfishnessHandler(IGlobalSettingsClient settingsClient, SelfOwnerShipCalculator calculator, ILiveClient liveClient, IVerifiedPLEntriesRepository plRepo, ILogger<SeedSelfishnessHandler> logger)
    {
        _settingsClient = settingsClient;
        _calculator = calculator;
        _liveClient = liveClient;
        _plRepo = plRepo;
        _logger = logger;
    }

    public async Task Handle(SeedSelfishness message, IMessageHandlerContext context)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Entry"] = message.EntryId });
        var verifiedPlEntry = await _plRepo.GetVerifiedPLEntry(message.EntryId);
        var settings = await _settingsClient.GetGlobalSettings();
        var allPlayers = settings.Players;
        var currentGameweekId = settings.Gameweeks.GetCurrentGameweek().Id;
        var liveItems = await GetAllLiveItems(currentGameweekId);
        var player = allPlayers.Get(verifiedPlEntry.PlayerId);
        var selfOwnership = (await _calculator.CalculateSelfOwnershipPoints(verifiedPlEntry.EntryId, player.Id, Enumerable.Range(1, currentGameweekId), liveItems)).ToArray();
        await _plRepo.UpdateStats(verifiedPlEntry.EntryId, verifiedPlEntry.SelfOwnershipStats with
        {
            TotalPoints = selfOwnership.Sum(),
            WeekCount = selfOwnership.Length,
            Gameweek = currentGameweekId
        });
        _logger.LogInformation("Seeding selfishness for {entry} finished", message.EntryId);
    }

    private async Task<ICollection<LiveItem>[]> GetAllLiveItems(int currentGameweek)
    {
        var liveItems = await Task.WhenAll(GetGameweekNumbersUpUntilCurrent(currentGameweek)
            .Select(gw => _liveClient.GetLiveItems(gw, gw == currentGameweek)));
        return liveItems;
    }

    private static IEnumerable<int> GetGameweekNumbersUpUntilCurrent(int gameweek)
    {
        return Enumerable.Range(1, gameweek);
    }
}
