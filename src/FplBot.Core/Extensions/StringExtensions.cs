using System;
using System.Collections.Generic;
using System.Linq;
using FplBot.Core.Abstractions;

namespace FplBot.Core.Extensions
{
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

            var firstSpaceIndex = s.IndexOf(' ');
            if (s.Length <= firstSpaceIndex + 1)
            {
                return string.Empty;
            }

            return s.Substring(firstSpaceIndex + 1).Trim();
        }
    }
}
