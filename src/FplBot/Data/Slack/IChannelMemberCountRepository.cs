namespace FplBot.Data.Slack;

public record ChannelMemberCount(int MemberCount, DateTimeOffset UpdatedAt);

public interface IChannelMemberCountRepository
{
    Task SetMemberCount(string channelId, int memberCount);
    Task Delete(string channelId);
    Task<IReadOnlyDictionary<string, ChannelMemberCount>> GetAll();
}
