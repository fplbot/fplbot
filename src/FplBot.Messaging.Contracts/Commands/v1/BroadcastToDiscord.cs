using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1;

[TimeToBeReceived("00:5:00")] // discard events not being handled within 30 mins
public record BroadcastToDiscord(string Message, ChannelFilter? Filter) : ICommand;

public enum ChannelFilter
{
    NotSet,
    AllChannels,
    AllChannelsDevServer,
    OnlyChannelsFollowingALeagueDevServer,
    OnlyChannelsFollowingALeague
}
