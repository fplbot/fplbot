using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FplBot.WebApi
{
    public static class CorsOriginValidator
    {
        public const string CustomCorsPolicyName = "CustomDynamicHerokuReviewAppsCompliantCorsPolicy";
        
        public static List<string> FixedOrigins = new List<string>
        {
            "http://localhost:3000", 
            "https://fplbot-frontend.herokuapp.com", 
            "https://www.fplbot.app"
        };
        
        private static Regex HerokuReviewAppsOriginRegex = new Regex("https:\\/\\/fplbotfrontend-pr-\\d+.herokuapp.com");

        public static bool ValidateOrigin(string origin)
        {
            var isAFixedEnv = FixedOrigins.Any(o => o.Equals(origin, StringComparison.InvariantCultureIgnoreCase));
            return isAFixedEnv || HerokuReviewAppsOriginRegex.IsMatch(origin);
        }
    }
}