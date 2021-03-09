using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;

using FplBot.Core.Helpers;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using MediatR;

namespace FplBot.Core.Handlers.InternalCommands
{
    public record UpdateSelfishStats(int Gameweek) : INotification;
    public record UpdateSelfishStatsForPLEntry(int Gameweek, int PLEntryId) : INotification;

    internal class UpdateSelfishStatsCommandHandler : INotificationHandler<UpdateSelfishStats>, INotificationHandler<UpdateSelfishStatsForPLEntry>
    {
        private readonly IVerifiedPLEntriesRepository _repo;
        private readonly ILiveClient _liveClient;
        private readonly SelfOwnerShipCalculator _calculator;
        private readonly IMediator _mediator;

        public UpdateSelfishStatsCommandHandler(IVerifiedPLEntriesRepository repo, ILiveClient liveClient, SelfOwnerShipCalculator calculator, IMediator mediator)
        {
            _repo = repo;
            _liveClient = liveClient;
            _calculator = calculator;
            _mediator = mediator;
        }

        public async Task Handle(UpdateSelfishStats notification, CancellationToken cancellationToken)
        {
            var plEntries = await _repo.GetAllVerifiedPLEntries();
            var liveItems = await _liveClient.GetLiveItems(notification.Gameweek);
            foreach (VerifiedPLEntry plEntry in plEntries)
            {
                await UpdateSelfishStatsForPLEntry(notification.Gameweek, cancellationToken, liveItems, plEntry);
            }
        }

        public async Task Handle(UpdateSelfishStatsForPLEntry notification, CancellationToken cancellationToken)
        {
            var plEntries = await _repo.GetAllVerifiedPLEntries();
            var liveItems = await _liveClient.GetLiveItems(notification.Gameweek);

            var plEntry = plEntries.Single(p => p.EntryId == notification.PLEntryId);

            await UpdateSelfishStatsForPLEntry(notification.Gameweek, cancellationToken, liveItems, plEntry);
        }

        private async Task UpdateSelfishStatsForPLEntry(int gameweek, CancellationToken cancellationToken, ICollection<LiveItem> liveItems, VerifiedPLEntry plEntry)
        {
            var gameweeks = Enumerable.Range(gameweek, 1);
            var nullFillers = Enumerable.Repeat<ICollection<LiveItem>>(null, gameweek - 1).ToList();
            nullFillers.Add(liveItems);
            var pointsForSelfPick =
                await _calculator.CalculateSelfOwnershipPoints(plEntry.EntryId, plEntry.PlayerId, gameweeks,
                    nullFillers.ToArray());
            await _mediator.Publish(
                new IncrementPointsFromSelfOwnership(EntryId: plEntry.EntryId, PointsFromSelf: pointsForSelfPick.Sum()),
                cancellationToken);
            if (pointsForSelfPick.Any())
            {
                await _mediator.Publish(new IncrementSelfOwnershipWeekCounter(EntryId: plEntry.EntryId), cancellationToken);
            }
        }
    }
}
