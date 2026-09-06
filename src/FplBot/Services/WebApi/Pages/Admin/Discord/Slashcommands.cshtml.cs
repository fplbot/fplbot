using Discord.Net.HttpClients;
using FplBot.Discord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FplBot.WebApi.Pages.Admin.Discord;

public class Slashcommands(DiscordSlashCommandsEnsurer ensurer) : PageModel
{
    private const string TestGuildId = "893932860162064414";

    public async Task OnGet()
    {
        Commands = await ensurer.GetAllForGuild(TestGuildId);
    }

    public IEnumerable<DiscordClient.ApplicationsCommand> Commands { get; set; } = null!;

    public async Task<IActionResult> OnPostUninstallSlashCommands()
    {
        TempData["msg"]+= "Uninstall queued!";
        await ensurer.DeleteGuildSlashCommands(TestGuildId);
        return RedirectToPage("Slashcommands");
    }

    public async Task<IActionResult> OnPostInstallSlashCommands()
    {
        TempData["msg"]+= "Install queued!";
        await ensurer.InstallGuildSlashCommandsInGuild(TestGuildId);
        return RedirectToPage("Slashcommands");
    }

    public async Task<IActionResult> OnPostInstallGlobalSlashCommands()
    {
        TempData["msg"]+= "Global install queued!";
        await ensurer.InstallGuildSlashCommandsInGuild();
        return RedirectToPage("Slashcommands");
    }
}
