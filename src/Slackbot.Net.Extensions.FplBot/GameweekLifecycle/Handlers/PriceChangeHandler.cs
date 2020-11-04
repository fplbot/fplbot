using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    public class PriceChangeHandler
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        public PriceChangeHandler(ISlackWorkSpacePublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task OnPriceChanges(GameweekLeagueContext ctx, IEnumerable<PriceChange> priceChanges)
        {
            if (ctx.SlackTeam.Subscriptions.ContainsSubscriptionFor(EventSubscription.PriceChanges))
            {
                var formatted = Formatter.FormatPriceChanged(priceChanges);
                await _publisher.PublishToWorkspace(ctx.SlackTeam.TeamId, "#johntest", formatted);
            }
        }
    }
}