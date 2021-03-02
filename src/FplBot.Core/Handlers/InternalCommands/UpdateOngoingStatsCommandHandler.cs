using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Data;
using MediatR;

namespace FplBot.Core.Handlers.InternalCommands
{
    public record UpdateEntryLiveStatsCommand : INotification;
    
    public class UpdateOngoingStatsCommandHandler : INotificationHandler<UpdateEntryLiveStatsCommand>
    {
        private readonly IEntryHistoryClient _entryHistoryClient;
        private readonly IVerifiedEntriesRepository _verifiedEntriesRepository;

        public UpdateOngoingStatsCommandHandler(IEntryHistoryClient entryHistoryClient, IVerifiedEntriesRepository verifiedEntriesRepository)
        {
            _entryHistoryClient = entryHistoryClient;
            _verifiedEntriesRepository = verifiedEntriesRepository;
        }

        public async Task Handle(UpdateEntryLiveStatsCommand notification, CancellationToken cancellationToken)
        {
            var allEntries = await _verifiedEntriesRepository.GetAllVerifiedEntries();
            
            foreach (var entry in allEntries)
            {
                var newStats = await GetLiveStatsForEntry(entry);
                await _verifiedEntriesRepository.UpdateLiveStats(entry.EntryId, newStats);
            }
        }

        private async Task<VerifiedEntryPointsUpdate> GetLiveStatsForEntry(VerifiedEntry entry)
        {
            var history = await _entryHistoryClient.GetHistory(entry.EntryId);
            var latestReportedGameweek = history.GameweekHistory.LastOrDefault();
            return new VerifiedEntryPointsUpdate(
                CurrentGwTotalPoints: latestReportedGameweek.TotalPoints,
                OverallRank: latestReportedGameweek.OverallRank ?? 0,
                PointsThisGw: latestReportedGameweek.Points);
        }
    }
}