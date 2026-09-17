using Discord.Net.HttpClients.Components;

namespace Discord.Net.Endpoints.Hosting;

public class ChannelMessageWithSourceComponentsResponse : SlashCommandResponse
{
    public ChannelMessageWithSourceComponentsResponse(ComponentRequest request)
    {
        Type = 4;
        Components = request.Components;
        Flags = ComponentRequest.IsComponentsV2;
    }

    public IReadOnlyList<MessageComponent> Components { get; set; }

    public int Flags { get; set; }
}
