namespace FplBot.Messaging.Contracts.Commands.v1;

public record BroadcastToDiscord(string Message, ChannelFilter? Filter);

public enum ChannelFilter
{
    NotSet,
    AllChannels,
    AllChannelsDevServer,
    OnlyChannelsFollowingALeagueDevServer,
    OnlyChannelsFollowingALeague
}
