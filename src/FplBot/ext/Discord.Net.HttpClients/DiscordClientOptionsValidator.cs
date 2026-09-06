using FluentValidation;

namespace Discord.Net.HttpClients;

public class DiscordClientOptionsValidator : AbstractValidator<DiscordClientOptions>
{
    public DiscordClientOptionsValidator()
    {
        RuleFor(x => x.DiscordApplicationId).NotEmpty().WithMessage("DiscordAppId is required");
        RuleFor(x => x.DiscordAppToken).NotEmpty().WithMessage("DISCORD_TOKEN is required");
    }
}
