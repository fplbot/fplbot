using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Pages.Admin
{
    public class Index : PageModel
    {
        private readonly ITokenStore _tokenStore;
        private readonly ISlackClientBuilder _builder;
        private readonly IOptions<SlackAppOptions> _slackAppOptions;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILogger<Index> _logger;

        public Index(ISlackClientBuilder builder, IOptions<SlackAppOptions> slackAppOptions, ISlackTeamRepository teamRepo, ITokenStore tokenStore, ILogger<Index> logger)
        {
            _builder = builder;
            _slackAppOptions = slackAppOptions;
            _teamRepo = teamRepo;
            _tokenStore = tokenStore;
            _logger = logger;
            Workspaces = new List<SlackTeam>();
        }
        
        public async Task OnGet()
        {
            await foreach (var t in _teamRepo.GetAllTeams())
            {
                Workspaces.Add(t);
            }
        }
        
        public async Task<IActionResult> OnPost(string teamId)
        {
            _logger.LogInformation($"Deleting {teamId}");
            var token = await _tokenStore.GetTokenByTeamId(teamId);
            var slackClient = _builder.Build(token: token);
            var res = await slackClient.AppsUninstall(_slackAppOptions.Value.Client_Id, _slackAppOptions.Value.Client_Secret);
            if (res.Ok)
            {
                TempData["msg"] = "Uninstall queued, and will be handled at some point";
            }
            else
            {
                TempData["msg"] = $"Uninstall failed '{res.Error}'";
            }
            
            return RedirectToPage("Index");
        }

        public List<SlackTeam> Workspaces { get; set; }
    }
}