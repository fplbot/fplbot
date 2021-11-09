namespace Discord.Net.Endpoints.Hosting;

public class ChannelMessageWithSourceEmbedResponse : SlashCommandResponse
{
    public ChannelMessageWithSourceEmbedResponse()
    {
        Type = 4;
        Embeds = new List<RichEmbed>();
    }

    public List<RichEmbed> Embeds { get; set; }
}

public record RichEmbed(string Title, string Description, int Color = 3604540, string Type = "rich");