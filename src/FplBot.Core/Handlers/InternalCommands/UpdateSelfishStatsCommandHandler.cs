using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Data;
using FplBot.Core.Helpers;
using MediatR;

namespace FplBot.Core.Handlers.InternalCommands
{
    public record UpdateSelfishStats(int Gameweek) : INotification;

    internal class UpdateSelfishStatsCommandHandler : INotificationHandler<UpdateSelfishStats>
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
                var gameweeks = Enumerable.Range(notification.Gameweek, 1);
                var nullFillers = Enumerable.Repeat<ICollection<LiveItem>>(null, notification.Gameweek - 1).ToList();
                nullFillers.Add(liveItems);
                var pointsForSelfPick = await _calculator.CalculateSelfOwnershipPoints(plEntry.EntryId, plEntry.PlayerId, gameweeks, nullFillers.ToArray());
                await _mediator.Publish(new IncrementPointsFromSelfOwnership(EntryId:plEntry.EntryId, PointsFromSelf: pointsForSelfPick.Sum()), cancellationToken);
                if (pointsForSelfPick.Any())
                {
                    await _mediator.Publish(new IncrementSelfOwnershipWeekCounter(EntryId:plEntry.EntryId), cancellationToken);    
                }
            }
        }
    }
}