using System;

namespace FplBot.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime WithOffset(this DateTime dateToConvertTime, int offsetInSeconds)
        {
            return dateToConvertTime.AddSeconds(offsetInSeconds);
        }
    }
}
