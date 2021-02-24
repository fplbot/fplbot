using System.Threading;
using System.Threading.Tasks;
using FplBot.Core.Data;
using MediatR;

namespace FplBot.Core.Handlers.InternalCommands
{
    public record IncrementSelfOwnershipWeekCounter(int EntryId) : INotification;

    public class IncrementSelfOwnershipWeekCounterCommandHandler : INotificationHandler<IncrementSelfOwnershipWeekCounter>
    {
        private readonly IVerifiedPLEntriesRepository _repo;

        public IncrementSelfOwnershipWeekCounterCommandHandler(IVerifiedPLEntriesRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(IncrementSelfOwnershipWeekCounter notification, CancellationToken cancellationToken)
        {
            var plEntry = await _repo.GetVerifiedPLEntry(notification.EntryId);
            var selfOwnership = plEntry.SelfOwnershipStats with { WeekCount = plEntry.SelfOwnershipStats.WeekCount + 1 };
            await _repo.UpdateStats(notification.EntryId, selfOwnership);
        }
    }
}