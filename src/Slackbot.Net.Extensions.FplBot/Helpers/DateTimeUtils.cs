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
            return isAboveThreshold && isBeforeDate && isSameMinute;
        }

        private DateTime NowUtc => NowUtcOverride ?? DateTime.UtcNow;
    }
}