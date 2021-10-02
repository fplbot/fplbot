using System.Threading.Tasks;
using Discord.Net.HttpClients;
using FplBot.Discord.Data;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Discord.Handlers.FplEvents
{
    public class NearDeadlineHandler :
        IHandleMessages<OneHourToDeadline>,
        IHandleMessages<PublishNearDeadlineToGuild>
    {
        private readonly IGuildRepository _teamRepo;
        private readonly DiscordClient _discordClient;

        private readonly ILogger<NearDeadlineHandler> _logger;

        public NearDeadlineHandler(IGuildRepository teamRepo, DiscordClient discordClient, ILogger<NearDeadlineHandler> logger)
        {
            _teamRepo = teamRepo;
            _discordClient = discordClient;
            _logger = logger;
        }

        public async Task Handle(OneHourToDeadline message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Notifying about 60 minutes to (gw{message.GameweekNearingDeadline.Id}) deadline");
            var allGuilds = await _teamRepo.GetAllGuildSubscriptions();
            foreach (var guild in allGuilds)
            {
                await context.SendLocal(new PublishNearDeadlineToGuild(guild.GuildId, guild.ChannelId, message.GameweekNearingDeadline.Id));
            }
        }

        public async Task Handle(PublishNearDeadlineToGuild message, IMessageHandlerContext context)
        {
            var text = $"@here ⏳Gameweek {message.GameweekId} deadline in 60 minutes!";
            await _discordClient.ChannelMessagePost(message.Channel, text);
        }
    }
}
