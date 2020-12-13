using System;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class DateTimeUtils
    {
        internal DateTime? NowUtcOverride;

        public bool IsWithinMinutesToDate(int minutes, DateTime dateUtc)
        {
            var threshold = dateUtc.Subtract(TimeSpan.FromMinutes(minutes));
            var isAboveThreshold = NowUtc >= threshold;
            var isBeforeDate = NowUtc < dateUtc;
            var isSameMinute = NowUtc.Minute == threshold.Minute;
            var isSameHour = NowUtc.Hour == threshold.Hour;
            return isAboveThreshold && isBeforeDate && isSameMinute && isSameHour;
        }

        private DateTime NowUtc => NowUtcOverride ?? DateTime.UtcNow;
    }
}