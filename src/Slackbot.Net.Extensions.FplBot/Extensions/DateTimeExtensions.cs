using System;

namespace Slackbot.Net.Extensions.FplBot.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime WithOffset(this DateTime dateToConvertTime, int offsetInSeconds)
        {
            return dateToConvertTime.AddSeconds(offsetInSeconds);
        }
    }
}
