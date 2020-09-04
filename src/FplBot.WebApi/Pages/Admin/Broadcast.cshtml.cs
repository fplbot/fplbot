using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace FplBot.WebApi.Pages.Admin
{
    public class Broadcast : PageModel
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILogger<Broadcast> _logger;

        public Broadcast(ISlackTeamRepository teamRepo, ISlackWorkSpacePublisher publisher, ILogger<Broadcast> logger)
        {
            _teamRepo = teamRepo;
            _publisher = publisher;
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
        
        public async Task<IActionResult> OnPost(string message)
        {
            _logger.LogInformation($"BROADCASTING TO ALL WORKSPACES");
            try
            {
                await _publisher.PublishToAllWorkspaceChannels(message);
                TempData["msg"] = "Broadcasted!";
            }
            catch (Exception e)
            {
                TempData["msg"] = $"Broadcast failed '{e}'";
            }

            return RedirectToPage("Broadcast");
        }
        
        public List<SlackTeam> Workspaces { get; set; }
    }
}