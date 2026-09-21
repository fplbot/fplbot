namespace FplBot.Data.Discord;

public record GuildMemberCount(int ApproximateMemberCount, DateTimeOffset UpdatedAt);

public interface IGuildMemberCountRepository
{
    Task SetApproximateMemberCount(string guildId, int approximateMemberCount);
    Task Delete(string guildId);
    Task<IReadOnlyDictionary<string, GuildMemberCount>> GetAll();
}
