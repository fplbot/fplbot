using System.Linq;

namespace Slackbot.Net.Extensions.FplBot.Extensions
{
    public static class StringExtensions
    {
        public static string Abbreviated(this string text)
        {
            return string.Join("", text.Replace("-", " ").Split(" ").Select(s => s.First()));
        }
    }
}
