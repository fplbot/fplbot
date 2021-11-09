using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Slack.Extensions;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Helpers;
using MediatR;

namespace FplBot.WebApi.Handlers.Commands;

public record SeedSelfishness : INotification;

public class SeedSelfishnessHandler : INotificationHandler<SeedSelfishness>
{
    private readonly IGlobalSettingsClient _settingsClient;
    private readonly SelfOwnerShipCalculator _calculator;
    private readonly IVerifiedPLEntriesRepository _plRepo;
    private readonly ILiveClient _liveClient;

    public SeedSelfishnessHandler(IVerifiedPLEntriesRepository plRepo, ILiveClient liveClient, IGlobalSettingsClient settingsClient, SelfOwnerShipCalculator calculator)
    {
        _plRepo = plRepo;
        _liveClient = liveClient;
        _settingsClient = settingsClient;
        _calculator = calculator;
    }

    public async Task Handle(SeedSelfishness notification, CancellationToken cancellationToken)
    {
        var settings = await _settingsClient.GetGlobalSettings();
        var allPlayers = settings.Players;

        var allPlEntries = await _plRepo.GetAllVerifiedPLEntries();
        int currentGameweekId = settings.Gameweeks.GetCurrentGameweek().Id;
        var liveItems = await GetAllLiveItems(currentGameweekId);

        foreach (var verifiedPlEntry in allPlEntries)
        {
            var player = allPlayers.Get(verifiedPlEntry.PlayerId);
            var selfOwnership = (await _calculator.CalculateSelfOwnershipPoints(verifiedPlEntry.EntryId, player.Id, Enumerable.Range(1, currentGameweekId), liveItems)).ToArray();
            await _plRepo.UpdateStats(verifiedPlEntry.EntryId, verifiedPlEntry.SelfOwnershipStats with
            {
                TotalPoints = selfOwnership.Sum(),
                WeekCount = selfOwnership.Length,
                Gameweek = currentGameweekId
            });
        }
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