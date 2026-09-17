namespace FplBot.Discord;

public static class ChannelPermissions
{
    private const long SendMessages = 1L << 11;

    public static string? Problem(long appPermissions) => appPermissions switch
    {
        0 => "I couldn't read my own permissions for this channel, so I can't tell you whether notifications will get through. Run `/help` again in a moment.",
        _ when Lacks(appPermissions, SendMessages) =>
            "@fplbot can't post in this channel. Give its role the **Send Messages** permission here.",
        _ => null
    };

    private static bool Lacks(long appPermissions, long permission) => (appPermissions & permission) == 0;
}
