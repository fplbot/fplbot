using System;
using System.Collections.Generic;
using System.Linq;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;

namespace FplBot.Core.Helpers
{
    public static class SlackHandleHelper
    {
        public static string GetSlackHandleOrFallback(IEnumerable<User> users, string entryName)
        {
            return GetSlackHandle(users, entryName) ?? entryName;
        }
        public static string GetSlackHandle(IEnumerable<User> users, string entryName)
        {
            return SearchForHandle(users, user => user.Real_name, entryName) ??
                   SearchForHandle(users, user => user.Real_name?.Split(" ")?.First(), entryName?.Split(" ")?.First()) ??
                   SearchForHandle(users, user => user.Real_name?.Split(" ")?.Last(), entryName?.Split(" ")?.Last());
        }

        private static string SearchForHandle(IEnumerable<User> users, Func<User, string> userProp, string searchFor)
        {
            var matches = users.Where(user => Equals(searchFor, userProp(user))).ToArray();
            if (matches.Length > 1)
            {
                // if more than one slack user has a name that matches the fpl entry,
                // we shouldn't @ either one of them, cause we're not sure who's the right one
                return null;
            }

            return matches.Length == 1 ? GetSlackHandle(matches.Single()) : null;
        }

        private static bool Equals(string s1, string s2)
        {
            if (s1 == null || s2 == null)
            {
                return false;
            }
            return string.Equals(s1, s2, StringComparison.CurrentCultureIgnoreCase);
        }

        private static string GetSlackHandle(User user) => $"<@{user.Id}>";
    }
}
