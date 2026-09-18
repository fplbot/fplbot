using System.Text;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.WebApi.Slack.Helpers;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class SubscribeCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    ISlackTeamRepository teamRepo,
    ILogger<SubscribeCommandHandler> logger)
    : IConsumer<ProcessSubscribeCommand>
{
    public async Task Consume(ConsumeContext<ProcessSubscribeCommand> context)
    {
        var command = context.Message;
        var subscriptionInfo = await GetAndUpdateSubscriptionInfo(command);
        await workspacePublisher.PublishToWorkspace(command.TeamId, command.ChannelId, subscriptionInfo);
    }

    private async Task<string> GetAndUpdateSubscriptionInfo(ProcessSubscribeCommand command)
    {
        try
        {
            var installation = await teamRepo.GetInstallation(command.TeamId);
            (var inputSubscriptions, var unableToParse) = ParseSubscriptionsFromInput(command.Text);

            if (inputSubscriptions.Count() < 1 && unableToParse.Count() < 1)
            {
                return $"You need to pass some arguments\n {FormatAllSubsAvailable()}";
            }

            if (inputSubscriptions.Count() < 1)
            {
                return $"I was not able to understand: *{string.Join(", ", unableToParse)}.* :confused: \n {FormatAllSubsAvailable()}";
            }

            var fplEvents = inputSubscriptions.Select(ToFplEvent).ToArray();

            if (command.Text.Contains("unsubscribe"))
            {
                installation.Unsubscribe(command.ChannelId, fplEvents);
            }
            else
            {
                installation.Subscribe(command.ChannelId, fplEvents);
            }

            await teamRepo.Save(installation);

            var channel = installation.GetChannel(command.ChannelId);
            var newSubscriptions = ToEventSubscriptions(channel?.Events.Current ?? []);
            return FormatSubscriptionMessage(newSubscriptions, unableToParse);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
            return "Oops, could not update subscriptions.";
        }
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());

    private static IEnumerable<EventSubscription> ToEventSubscriptions(IEnumerable<FplEvent> events) =>
        events.Select(e => Enum.Parse<EventSubscription>(e.ToString()));

    private static (IEnumerable<EventSubscription> events, string[] unableToParse) ParseSubscriptionsFromInput(string text)
    {
        var stringListOfEvents = MessageHelper.ExtractArgs(text, ["subscribe {args}", "unsubscribe {args}"]) ?? "";
        return stringListOfEvents.ParseSubscriptionString(delimiter: ",");
    }

    private static string FormatSubscriptionMessage(IEnumerable<EventSubscription> eventSubscriptions, string[] unableToParse)
    {
        var sb = new StringBuilder();

        sb.Append("Updated subscriptions :sparkles:\n");

        if (eventSubscriptions.Count() < 1) sb.Append("You are not subscribing to any fplbot updates.");
        else sb.Append($"You will now receive updates for:\n{Formatter.BulletPoints(eventSubscriptions)}");

        if (unableToParse.Any())
        {
            sb.Append("\n");
            sb.Append($"Btw, I was not able to understand these: *{string.Join(", ", unableToParse)}.* \n" + FormatAllSubsAvailable());
        }

        return sb.ToString();
    }

    private static string FormatAllSubsAvailable()
    {
        return $"You can choose from: {string.Join(", ", EventSubscriptionHelper.GetAllSubscriptionTypes())}";
    }
}
