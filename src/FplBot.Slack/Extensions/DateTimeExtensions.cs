using System;

namespace FplBot.Slack.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime WithOffset(this DateTime dateToConvertTime, int offsetInSeconds)
        {
            return dateToConvertTime.AddSeconds(offsetInSeconds);
        }
    }
}
