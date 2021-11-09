namespace Discord.Net.Endpoints.Hosting;

public record Guild(string Id, string Name);

public interface IGuildStore
{
    public Task Insert(Guild guild);
    public Task<Guild> DeleteGuild(string guildId);
}