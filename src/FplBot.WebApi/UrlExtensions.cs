using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FplBot.WebApi
{
    public static class IUrlHelperExtensions
    {
        public static string AbsoluteLink(this IUrlHelper helper, string host, string action)
        {
            return helper.ActionLink(action, host);
        }
    }
}