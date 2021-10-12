using System.Threading.Tasks;
using FplBot.Discord.Data;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Discord.Handlers.FplEvents
{
    public class LineupReadyHandler : IHandleMessages<LineupReady>
    {
        private readonly IGuildRepository _guildRepository;

        public LineupReadyHandler(IGuildRepository guildRepository)
        {
            _guildRepository = guildRepository;
        }

        public async Task Handle(LineupReady message, IMessageHandlerContext context)
        {
            var subs = await _guildRepository.GetAllGuildSubscriptions();
            var lineups = message.Lineup;
            var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* ";
            var formattedLineup = Formatter.FormatLineup(lineups);
            foreach (var sub in subs)
            {
                if (sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.Lineups))
                    await context.SendLocal(new PublishRichToGuildChannel(sub.GuildId, sub.ChannelId, $"ℹ️ {firstMessage}", $"{formattedLineup}"));
            }
        }
    }
}
