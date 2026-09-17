namespace Discord.Net.Endpoints.Hosting;

public class DeferredResponse : SlashCommandResponse
{
    public DeferredResponse()
    {
        Type = 5;
    }
}
