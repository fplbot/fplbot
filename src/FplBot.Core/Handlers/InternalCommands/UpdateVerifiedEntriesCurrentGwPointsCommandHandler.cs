using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.Handlers.InternalCommands
{
    public record UpdateVerifiedEntriesCurrentGwPointsCommand : INotification;

    public class UpdateVerifiedEntriesCurrentGwPointsCommandHandler : INotificationHandler<UpdateVerifiedEntriesCurrentGwPointsCommand>
    {
        private readonly IEntryClient _entryClient;
        private readonly IVerifiedEntriesRepository _verifiedEntriesRepository;
        private readonly ILogger<UpdateVerifiedEntriesCurrentGwPointsCommandHandler> _logger;

        public UpdateVerifiedEntriesCurrentGwPointsCommandHandler(IEntryClient entryClient, IVerifiedEntriesRepository verifiedEntriesRepository, ILogger<UpdateVerifiedEntriesCurrentGwPointsCommandHandler> logger)
        {
            _entryClient = entryClient;
            _verifiedEntriesRepository = verifiedEntriesRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateVerifiedEntriesCurrentGwPointsCommand notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating gameweek points for verified entries");
            var allEntries = await _verifiedEntriesRepository.GetAllVerifiedEntries();

            foreach (var entry in allEntries)
            {
                var newStats = await GetLiveStatsForEntry(entry);
                await _verifiedEntriesRepository.UpdateLiveStats(entry.EntryId, newStats);
            }
        }

        private async Task<VerifiedEntryPointsUpdate> GetLiveStatsForEntry(VerifiedEntry entry)
        {
            var basicEntry = await _entryClient.Get(entry.EntryId);

            return new VerifiedEntryPointsUpdate(
                CurrentGwTotalPoints: basicEntry.SummaryOverallPoints ?? 0,
                OverallRank: basicEntry.SummaryOverallRank ?? 0,
                PointsThisGw: basicEntry.SummaryEventPoints ?? 0);
        }
    }
}
