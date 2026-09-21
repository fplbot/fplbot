namespace FplBot.Data.Discord;

public interface IGuildMemberCountRepository
{
    Task SetApproximateMemberCount(string guildId, int approximateMemberCount);
    Task Delete(string guildId);
    Task<IReadOnlyDictionary<string, int>> GetAll();
}
