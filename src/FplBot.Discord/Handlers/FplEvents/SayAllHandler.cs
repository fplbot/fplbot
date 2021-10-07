using System.Threading.Tasks;
using Discord.Net.HttpClients;
using FplBot.Discord.Data;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Discord.Handlers.FplEvents
{
    public class SayAllHandler :
        IHandleMessages<GameweekJustBegan>,
        IHandleMessages<GameweekFinished>,
        IHandleMessages<InjuryUpdateOccured>,
        IHandleMessages<PlayersPriceChanged>,
        IHandleMessages<NewPlayersRegistered>,
        IHandleMessages<PublishToGuildChannel>
    {
        private readonly DiscordClient _discordClient;
        private readonly IGuildRepository _repo;
        private readonly ILogger<NearDeadlineHandler> _logger;

        public SayAllHandler(DiscordClient discordClient, IGuildRepository repo, ILogger<NearDeadlineHandler> logger)
        {
            _discordClient = discordClient;
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(GameweekJustBegan message, IMessageHandlerContext context)
        {
            await SendToGuilds(message,context);
        }

        public async Task Handle(GameweekFinished message, IMessageHandlerContext context)
        {
            await SendToGuilds(message,context);        }

        public async Task Handle(InjuryUpdateOccured message, IMessageHandlerContext context)
        {
            await SendToGuilds(message,context);        }

        public async Task Handle(PlayersPriceChanged message, IMessageHandlerContext context)
        {
            await SendToGuilds(message,context);        }

        public async Task Handle(NewPlayersRegistered message, IMessageHandlerContext context)
        {
            await SendToGuilds(message,context);
        }

        private async Task SendToGuilds(IEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation(message.ToString());
            var guildSubs = await _repo.GetAllGuildSubscriptions();
            foreach (GuildFplSubscription guildSub in guildSubs)
            {
                await context.SendLocal(new PublishToGuildChannel(guildSub.GuildId, guildSub.ChannelId, message.ToString()));
            }
        }

        public async Task Handle(PublishToGuildChannel message, IMessageHandlerContext context)
        {
            await _discordClient.ChannelMessagePost(message.ChannelId, message.Message);
        }
    }
}
