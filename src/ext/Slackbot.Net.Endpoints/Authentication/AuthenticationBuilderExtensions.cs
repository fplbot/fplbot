using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Slackbot.Net.Endpoints.Authentication
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddSlackbotEvents(this AuthenticationBuilder builder, Action<SlackbotEventsAuthenticationOptions> optionsAction)
        {
            builder.Services.Configure(optionsAction);
            return builder.AddScheme<SlackbotEventsAuthenticationOptions, SlackbotEventsAuthenticationAuthenticationHandler>(SlackbotEventsAuthenticationConstants.AuthenticationScheme, optionsAction);
        }
    }

    public class SlackbotEventsAuthenticationOptions : AuthenticationSchemeOptions
    {
        [Required]
        public string SigningSecret { get; set; }
    }

    public static class SlackbotEventsAuthenticationConstants
    {
        public const string AuthenticationScheme = "SlacbotEventsAuthenticationScheme";
    }
}
