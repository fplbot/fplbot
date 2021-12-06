namespace Fpl.Workers.Helpers;

public class DateTimeUtils
{
    public DateTime? NowUtcOverride;

    public bool IsWithinMinutesToDate(int minutes, DateTime dateUtc)
    {
        var threshold = dateUtc.Subtract(TimeSpan.FromMinutes(minutes));
        var isAboveThreshold = NowUtc >= threshold;
        var isBeforeDate = NowUtc < dateUtc;
        var isSameMinute = NowUtc.Minute == threshold.Minute;
        var isSameHour = NowUtc.Hour == threshold.Hour;
        return isAboveThreshold && isBeforeDate && isSameMinute && isSameHour;
    }

    public DateTime NowUtc => NowUtcOverride ?? DateTime.UtcNow;
}
