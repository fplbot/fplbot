using System.Threading;
using System.Threading.Tasks;
using FplBot.VerifiedEntries.Data.Abstractions;
using MediatR;

namespace FplBot.VerifiedEntries.InternalCommands
{
    public record IncrementPointsFromSelfOwnership(int EntryId, int PointsFromSelf) : INotification;

    public class IncrementSelfOwnershipPointsCommandHandler : INotificationHandler<IncrementPointsFromSelfOwnership>
    {
        private readonly IVerifiedPLEntriesRepository _repo;

        public IncrementSelfOwnershipPointsCommandHandler(IVerifiedPLEntriesRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(IncrementPointsFromSelfOwnership notification, CancellationToken cancellationToken)
        {
            var plEntry = await _repo.GetVerifiedPLEntry(notification.EntryId);
            var newPoints = plEntry.SelfOwnershipStats.TotalPoints + notification.PointsFromSelf;
            var selfOwnership = plEntry.SelfOwnershipStats with { TotalPoints = newPoints };
            await _repo.UpdateStats(notification.EntryId, selfOwnership);
        }
    }
}
