using System;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class DateTimeUtils
    {
        public bool IsWithinMinutesToDate(int minutes, DateTime dateUtc)
        {
            var threshold = dateUtc.Subtract(TimeSpan.FromMinutes(minutes));
            return NowUtc >= threshold && NowUtc.Minute == dateUtc.Minute;
        }

        public DateTime NowUtc { get; set; } = DateTime.UtcNow;
    }
}