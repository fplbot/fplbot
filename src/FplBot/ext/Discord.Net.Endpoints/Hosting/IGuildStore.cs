namespace Discord.Net.Endpoints.Hosting;

public record Guild(string Id, string Name);

public interface IGuildInstallationHandler
{
    public Task Install(Guild guild);
    public Task Uninstall(string guildId);
}
