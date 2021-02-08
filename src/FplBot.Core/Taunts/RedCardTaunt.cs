namespace Slackbot.Net.Extensions.FplBot.Taunts
{
    public class RedCardTaunt : ITaunt
    {
        public TauntType Type => TauntType.InTransfers;

        public string[] JokePool => new []
        {
            "Smart move bringing him in, {0} :upside_down_face:",
            "Didn't you transfer him in this week, {0}? :japanese_ogre:",
            "Maybe you should have waited a couple of more weeks before knee jerking him in, {0}?"
        };
    }
}