using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Data;
using FplBot.Core.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.WebApi.Handlers.Commands
{
    public record SeedVerifiedEntries : INotification;

    public class SeedEntriesHandler : INotificationHandler<SeedVerifiedEntries>
    {
        private readonly IEntryClient _entryClient;
        private readonly IGlobalSettingsClient _settings;
        private readonly IVerifiedEntriesRepository _repo;
        private readonly IVerifiedPLEntriesRepository _plRepo;
        private readonly ILogger<SeedEntriesHandler> _logger;

        public SeedEntriesHandler(IEntryClient entryClient, IGlobalSettingsClient settings, IVerifiedEntriesRepository repo, IVerifiedPLEntriesRepository plRepo, ILogger<SeedEntriesHandler> logger)
        {
            _entryClient = entryClient;
            _settings = settings;
            _repo = repo;
            _plRepo = plRepo;
            _logger = logger;
        }
        
        public async Task Handle(SeedVerifiedEntries notification, CancellationToken cancellationToken)
        {
            var settings = await _settings.GetGlobalSettings();
            _logger.LogInformation($"Inserting hardcoded list");

            var allTeams = settings.Teams.ToList();
            var allPlayers = settings.Players.ToList();

            foreach (var keyVal in Fpl.Search.VerifiedEntries.VerifiedEntriesMap)
            {
                var entriesFromApi = await _entryClient.Get(keyVal.Key);

                await _repo.Insert(new VerifiedEntry(
                    EntryId: keyVal.Key,
                    FullName: entriesFromApi.PlayerFullName,
                    EntryTeamName: entriesFromApi.TeamName,
                    VerifiedEntryType: keyVal.Value));

                var verifiedPlEntry = Fpl.Search.VerifiedEntries.VerifiedPLEntries.SingleOrDefault(x => x.EntryId == keyVal.Key);
                if (verifiedPlEntry != null)
                {
                    var plPlayer = allPlayers.Get(verifiedPlEntry.PlayerId);
                    _logger.LogInformation($"FOUND PLAYER {plPlayer.WebName}, INSERTING PL ENTRY {keyVal.Key}");
                    var teamForPLEntry = allTeams.Get(plPlayer?.TeamId);

                    await _plRepo.Insert(new VerifiedPLEntry(
                        EntryId: keyVal.Key,
                        TeamId: teamForPLEntry.Id,
                        TeamCode: teamForPLEntry.Code,
                        TeamName: teamForPLEntry.Name,
                        PlayerId: plPlayer.Id,
                        PlayerCode: plPlayer.Code,
                        PlayerWebName: plPlayer.WebName,
                        PlayerFullName: plPlayer.FullName
                    ));
                }
                else
                {
                    _logger.LogWarning($"NO PL ENTRY FOR {keyVal.Key}");
                }

                _logger.LogInformation($"Inserting {keyVal.Key}");
            }

            _logger.LogInformation($"All inserted");
        }
    }
}