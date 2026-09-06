using FluentValidation;

namespace FplBot.WebApi.Configurations;

public class SlackAdminOptionsValidator : AbstractValidator<SlackAdminOptions>
{
    public SlackAdminOptionsValidator()
    {
        RuleFor(x => x.SlackClientId).NotEmpty().WithMessage("admin:SlackClientId is required");
        RuleFor(x => x.SlackClientSecret).NotEmpty().WithMessage("admin:SlackClientSecret is required");
    }
}
