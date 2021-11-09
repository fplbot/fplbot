using System;

namespace FplBot.Formatting;

public static class DateTimeExtensions
{
    public static DateTime WithOffset(this DateTime dateToConvertTime, int offsetInSeconds)
    {
        return dateToConvertTime.AddSeconds(offsetInSeconds);
    }
}