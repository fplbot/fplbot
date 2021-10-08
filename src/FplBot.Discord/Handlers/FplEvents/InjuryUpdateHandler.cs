using System.Linq;
using System.Threading.Tasks;
using FplBot.Discord.Data;
using FplBot.Discord.Extensions;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Discord.Handlers.FplEvents
{
    public class InjuryUpdateHandler : IHandleMessages<InjuryUpdateOccured>
    {
        private readonly IGuildRepository _repo;
        private readonly ILogger<InjuryUpdateHandler> _logger;

        public InjuryUpdateHandler(IGuildRepository repo, ILogger<InjuryUpdateHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(InjuryUpdateOccured message, IMessageHandlerContext context)
        {
            var guildSubs = await _repo.GetAllGuildSubscriptions();
            var filtered = message.PlayersWithInjuryUpdates.Where(c => c.Player.IsRelevant());

            if (filtered.Any())
            {
                var formatted = Formatter.FormatInjuryStatusUpdates(filtered);

                foreach (var guildSub in guildSubs)
                {
                    if (guildSub.Subscriptions.ContainsSubscriptionFor(EventSubscription.InjuryUpdates) && !string.IsNullOrEmpty(formatted))
                    {
                        await context.SendLocal(new PublishToGuildChannel(guildSub.GuildId, guildSub.ChannelId, formatted));
                    }
                }
            }
        }
    }
}
