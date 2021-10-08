namespace Discord.Net.Endpoints.Hosting
{
    public class ChannelMessageWithSourceResponse : SlashCommandResponse
    {
        public ChannelMessageWithSourceResponse()
        {
            Type = 4;
        }

        public string Content { get; set; }
    }

    public record Embed(string Title, string Description, int Color = 3604540);
}
