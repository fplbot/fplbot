namespace FplBot.Slack.Extensions;

public static class StringExtensions
{
    public static string Abbreviated(this string text)
    {
        return string.Join("", text.Replace("-", " ").Split(" ").Select(s => s.First()));
    }

    public static string TextAfterFirstSpace(this string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return string.Empty;
        }

        // non breaking white space may occur if text is copy pasted
        var firstSpaceIndex = s.IndexOf("\u00a0", StringComparison.InvariantCultureIgnoreCase);
        if (firstSpaceIndex == -1)
        {
            firstSpaceIndex = s.IndexOf(' ');
        }

        if (s.Length <= firstSpaceIndex + 1)
        {
            return string.Empty;
        }

        return s.Substring(firstSpaceIndex + 1).Trim();
    }
}