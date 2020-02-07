using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace FplBot.WebApi.Pages
{
    public class Install : PageModel
    {
        public Install(IOptions<DistributedSlackAppOptions> opts)
        {
            ClientId = opts.Value.CLIENT_ID;
        }
    
        public void OnGet()
        {
            
        }

        public string ClientId { get; set; }
    }
}