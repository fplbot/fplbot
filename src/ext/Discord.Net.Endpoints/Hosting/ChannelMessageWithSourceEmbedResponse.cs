namespace Discord.Net.Endpoints.Hosting
{
    public class ChannelMessageWithSourceEmbedResponse : SlashCommandResponse
    {
        public ChannelMessageWithSourceEmbedResponse()
        {
            Type = 4;
        }

        public Embed Embed { get; set; }
    }
}
