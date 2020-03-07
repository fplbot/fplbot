using System;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class DateTimeUtils
    {
        internal DateTime? NowUtcOverride;

        public bool IsWithinMinutesToDate(int minutes, DateTime dateUtc)
        {
            var threshold = dateUtc.Subtract(TimeSpan.FromMinutes(minutes));
            return NowUtc >= threshold && NowUtc < dateUtc && NowUtc.Minute == dateUtc.Minute;
        }

        private DateTime NowUtc => NowUtcOverride ?? DateTime.UtcNow;
    }
}