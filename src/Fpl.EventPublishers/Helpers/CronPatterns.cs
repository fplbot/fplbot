namespace Fpl.EventPublishers.Helpers;

public static class CronPatterns
{
    public const string EveryMinute = "0 */1 * * * *";
    public const string EveryOtherMinute = "0 */2 * * * *";
    public const string EveryFiveMinutesAt40seconds = "40 */5 * * * *";
    public const string EveryMinuteAt20Seconds = "20 */1 * * * *";
    public const string EveryTwentySeconds = "*/20 * * * * *";
    public const string EveryFifteenSeconds = "*/15 * * * * *";
    public const string EveryOtherMinuteAt40SecondsSharp = "40 */2 * * * *";
}
