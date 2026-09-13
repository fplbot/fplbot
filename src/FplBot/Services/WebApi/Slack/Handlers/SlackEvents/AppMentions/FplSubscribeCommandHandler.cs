using System.Text;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Services.WebApi.Slack.Abstractions;
using FplBot.Services.WebApi.Slack.Helpers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

internal class FplSubscribeCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    ISlackTeamRepository teamRepo,
    ILogger<FplSubscriptionsCommandHandler> logger)
    : HandleAppMentionBase
{
    public override string[] Commands => ["subscribe", "unsubscribe"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
    {
        var subscriptionInfo = await GetAndUpdateSubscriptionInfo(eventMetadata.Team_Id, appMentioned);
        await workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, appMentioned.Channel, subscriptionInfo);
        return new EventHandledResponse(subscriptionInfo);
    }

    private async Task<string> GetAndUpdateSubscriptionInfo(string teamId, AppMentionEvent appMentioned)
    {
        try
        {
            var installation = await teamRepo.GetInstallation(teamId);
            (var inputSubscriptions, var unableToParse) = ParseSubscriptionsFromInput(appMentioned);

            if (inputSubscriptions.Count() < 1 && unableToParse.Count() < 1)
            {
                return $"You need to pass some arguments\n {FormatAllSubsAvailable()}";
            }

            if (inputSubscriptions.Count() < 1)
            {
                return $"I was not able to understand: *{string.Join(", ", unableToParse)}.* :confused: \n {FormatAllSubsAvailable()}";
            }

            var fplEvents = inputSubscriptions.Select(ToFplEvent).ToArray();

            if (appMentioned.Text.Contains("unsubscribe"))
            {
                installation.Unsubscribe(appMentioned.Channel, fplEvents);
            }
            else
            {
                installation.Subscribe(appMentioned.Channel, fplEvents);
            }

            await teamRepo.Save(installation);

            var channel = installation.GetChannel(appMentioned.Channel);
            var newSubscriptions = ToEventSubscriptions(channel?.Events.Current ?? []);
            return FormatSubscriptionMessage(newSubscriptions, unableToParse);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
            return $"Oops, could not update subscriptions.";
        }
    }

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());

    private static IEnumerable<EventSubscription> ToEventSubscriptions(IEnumerable<FplEvent> events) =>
        events.Select(e => Enum.Parse<EventSubscription>(e.ToString()));

    private static (IEnumerable<EventSubscription> events, string[] unableToParse) ParseSubscriptionsFromInput(AppMentionEvent appMentioned)
    {
        var stringListOfEvents = MessageHelper.ExtractArgs(appMentioned.Text, ["subscribe {args}", "unsubscribe {args}"
        ]) ?? "";
        return stringListOfEvents.ParseSubscriptionString(delimiter: ",");
    }

    private string FormatSubscriptionMessage(IEnumerable<EventSubscription> eventSubscriptions, string[] unableToParse)
    {
        var sb = new StringBuilder();

        sb.Append("Updated subscriptions :sparkles:\n");

        if (eventSubscriptions.Count() < 1) sb.Append($"You are not subscribing to any fplbot updates.");
        else sb.Append($"You will now receive updates for:\n{Formatter.BulletPoints(eventSubscriptions)}");

        if (unableToParse.Any())
        {
            sb.Append("\n");
            sb.Append($"Btw, I was not able to understand these: *{string.Join(", ", unableToParse)}.* \n" + FormatAllSubsAvailable());
        }

        return sb.ToString();
    }

    private string FormatAllSubsAvailable()
    {
        return $"You can choose from: {string.Join(", ", EventSubscriptionHelper.GetAllSubscriptionTypes())}";
    }

    public override (string, string) GetHelpDescription()
    {
        var sb = new StringBuilder();

        sb.Append("Update what notifications fplbot should post. (");

        sb.Append($"{string.Join(", ", EventSubscriptionHelper.GetAllSubscriptionTypes())})");

        return (
            "subscribe/unsubscribe {comma separated list of events}",
            sb.ToString());
    }
}
