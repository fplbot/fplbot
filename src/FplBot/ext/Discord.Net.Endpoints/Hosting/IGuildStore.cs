namespace Discord.Net.Endpoints.Hosting;

public record Guild(string Id, string Name);

public interface IGuildInstallationHandler
{
    Task Install(Guild guild);
    Task Uninstall(string guildId);
}
