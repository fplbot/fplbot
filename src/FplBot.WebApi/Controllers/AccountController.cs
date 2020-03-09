using System.Threading.Tasks;
using AspNet.Security.OAuth.Slack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FplBot.WebApi.Controllers
{
    [Route("[controller]")]

    public class AccountController : Controller
    {
        [Route("/challenge")]
        public async Task<ChallengeResult> TriggerChallenge()
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = "/admin"
            });
        }
        
        [Route("/logout")]
        [AllowAnonymous]
        public async Task<RedirectToPageResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/SignedOut");
        }
    }
}