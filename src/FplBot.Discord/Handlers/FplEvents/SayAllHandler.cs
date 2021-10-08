using System.Threading.Tasks;
using FplBot.Discord.Data;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Discord.Handlers.FplEvents
{
    public class SayAllHandler : IHandleMessages<GameweekFinished>
    {
        private readonly IGuildRepository _repo;

        public SayAllHandler(IGuildRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(GameweekFinished message, IMessageHandlerContext context)
        {
            await SendToGuildsIfSubscribing(message,context);
        }

        private async Task SendToGuildsIfSubscribing(IEvent message, IMessageHandlerContext context)
        {
            var guildSubs = await _repo.GetAllGuildSubscriptions();
            foreach (GuildFplSubscription guildSub in guildSubs)
            {
                await context.SendLocal(new PublishToGuildChannel(guildSub.GuildId, guildSub.ChannelId, message.ToString()));
            }
        }
    }
}
