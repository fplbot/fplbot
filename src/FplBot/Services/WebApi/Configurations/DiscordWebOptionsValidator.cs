using FluentValidation;

namespace FplBot.WebApi.Configurations;

public class DiscordWebOptionsValidator : AbstractValidator<DiscordWebOptions>
{
    public DiscordWebOptionsValidator()
    {
        RuleFor(x => x.DISCORD_CLIENT_ID).NotEmpty().WithMessage("DISCORD_CLIENT_ID is required");
        RuleFor(x => x.DISCORD_CLIENT_SECRET).NotEmpty().WithMessage("DISCORD_CLIENT_SECRET is required");
        RuleFor(x => x.DISCORD_PUBLICKEY).NotEmpty().WithMessage("DISCORD_PUBLICKEY is required");
    }
}
