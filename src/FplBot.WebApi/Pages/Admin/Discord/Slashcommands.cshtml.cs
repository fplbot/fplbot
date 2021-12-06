using Discord.Net.HttpClients;
using FplBot.Discord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FplBot.WebApi.Pages.Admin.Discord;

public class Slashcommands : PageModel
{
    private const string TestGuildId = "893932860162064414";
    private readonly DiscordSlashCommandsEnsurer _ensurer;

    public Slashcommands(DiscordSlashCommandsEnsurer ensurer)
    {
        _ensurer = ensurer;
    }

    public async Task OnGet()
    {
        Commands = await _ensurer.GetAllForGuild(TestGuildId);
    }

    public IEnumerable<DiscordClient.ApplicationsCommand> Commands { get; set; }

    public async Task<IActionResult> OnPostUninstallSlashCommands()
    {
        TempData["msg"]+= "Uninstall queued!";
        await _ensurer.DeleteGuildSlashCommands(TestGuildId);
        return RedirectToPage("Slashcommands");
    }

    public async Task<IActionResult> OnPostInstallSlashCommands()
    {
        TempData["msg"]+= "Install queued!";
        await _ensurer.InstallGuildSlashCommandsInGuild(TestGuildId);
        return RedirectToPage("Slashcommands");
    }

    public async Task<IActionResult> OnPostInstallGlobalSlashCommands()
    {
        TempData["msg"]+= "Global install queued!";
        await _ensurer.InstallGuildSlashCommandsInGuild();
        return RedirectToPage("Slashcommands");
    }
}
