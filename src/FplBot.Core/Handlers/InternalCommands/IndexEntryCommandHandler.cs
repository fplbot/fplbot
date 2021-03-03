using System.Threading;
using System.Threading.Tasks;
using Fpl.Search.Indexing;
using MediatR;

namespace FplBot.Core.Handlers.InternalCommands
{
    public record IndexEntry(int EntryId) : INotification;

    public class IndexEntryCommandHandler : INotificationHandler<IndexEntry>
    {
        private readonly IIndexingService _indexingService;

        public IndexEntryCommandHandler(IIndexingService indexingService)
        {
            _indexingService = indexingService;
        }

        public Task Handle(IndexEntry notification, CancellationToken cancellationToken)
        {
            return _indexingService.IndexSingleEntry(notification.EntryId, cancellationToken);
        }
    }
}
