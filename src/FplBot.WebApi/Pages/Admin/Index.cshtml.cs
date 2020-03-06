using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace FplBot.WebApi.Pages.Admin
{
    public class Index : PageModel
    {
        private readonly ITokenStore _tokenStore;
        private readonly IFetchFplbotSetup _setups;

        public Index(ITokenStore tokenStore, IFetchFplbotSetup setups)
        {
            _tokenStore = tokenStore;
            _setups = setups;
            Workspaces = new List<FplbotSetup>();
        }
        
        public async Task OnGet()
        {
            var tokens = await _tokenStore.GetTokens();
            foreach (var token in tokens)
            {
                var setup = await _setups.GetSetupByToken(token);
                Workspaces.Add(setup);
            }
        }

        public List<FplbotSetup> Workspaces { get; set; }
    }
}