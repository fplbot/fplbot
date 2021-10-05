using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Discord.Net.Endpoints.Authentication
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddDiscordbotEvents(this AuthenticationBuilder builder, Action<DiscordEventsAuthenticationOptions> optionsAction)
        {
            builder.Services.Configure(optionsAction);
            return builder.AddScheme<DiscordEventsAuthenticationOptions, DiscordEventsAuthenticationAuthenticationHandler>(DiscordEventsAuthenticationConstants.AuthenticationScheme, optionsAction);
        }
    }

    public class DiscordEventsAuthenticationOptions : AuthenticationSchemeOptions
    {
        [Required]
        public string PublicKey { get; set; }
    }

    public static class DiscordEventsAuthenticationConstants
    {
        public const string AuthenticationScheme = "DiscordEventsAuthenticationScheme";
    }
}
