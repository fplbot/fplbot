namespace Discord.Net.Endpoints.Middleware
{
    internal class HttpItemKeys
    {
        public const string TypeKey = "discordevents:type";
        public const string PingKey = "discordevents:ping";
        public const string SlashCommandsKey = "discordevents:slash";
        public const string UnhandledKey = "discordevents:unhandled";
        public static string RawBody = "discordevents:rawbody";
    }
}
