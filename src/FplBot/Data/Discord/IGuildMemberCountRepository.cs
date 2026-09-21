namespace FplBot.Data.Discord;

public record GuildMemberCount(int ApproximateMemberCount, DateTimeOffset UpdatedAt, bool IsCommunity);

public interface IGuildMemberCountRepository
{
    Task SetApproximateMemberCount(string guildId, int approximateMemberCount, bool isCommunity = false);
    Task Delete(string guildId);
    Task<IReadOnlyDictionary<string, GuildMemberCount>> GetAll();
}
