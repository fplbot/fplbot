using System.Threading.Tasks;
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
            await HttpContext.SignOutAsync();
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = "/admin"
            });
        }
        
        [Route("/logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            return SignOut();
        }
    }
}