using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.EventHandlers.Slack.Helpers;

public static class SlackInstallationExtensions
{
    public static SlackChannelSubscription? PrimaryChannel(this SlackInstallation installation) =>
        installation.ChannelSubscriptions.FirstOrDefault();

    public static bool HasChannelAndLeagueSetup(this SlackInstallation installation) =>
        installation.PrimaryChannel()?.FollowedLeagueId is not null;

    public static bool HasRegisteredFor(this SlackInstallation installation, FplEvent fplEvent) =>
        installation.HasChannelAndLeagueSetup() && installation.PrimaryChannel()!.IsSubscribedTo(fplEvent);

    public static bool ContainsStat(this SlackInstallation installation, StatType statType)
    {
        var channel = installation.PrimaryChannel();
        var fplEvent = statType.GetFplEvent();
        return channel is not null && fplEvent.HasValue && channel.IsSubscribedTo(fplEvent.Value);
    }

    private static FplEvent? GetFplEvent(this StatType statType) => statType switch
    {
        StatType.GoalsScored => FplEvent.FixtureGoals,
        StatType.Assists => FplEvent.FixtureAssists,
        StatType.OwnGoals => FplEvent.FixtureGoals,
        StatType.RedCards => FplEvent.FixtureCards,
        StatType.PenaltiesSaved => FplEvent.FixturePenaltyMisses,
        StatType.PenaltiesMissed => FplEvent.FixturePenaltyMisses,
        _ => null
    };
}
