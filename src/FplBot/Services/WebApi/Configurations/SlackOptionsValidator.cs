using FluentValidation;

namespace FplBot.WebApi.Configurations;

public class SlackOptionsValidator : AbstractValidator<SlackOptions>
{
    public SlackOptionsValidator()
    {
        RuleFor(x => x.CLIENT_ID).NotEmpty().WithMessage("Slack CLIENT_ID is required");
        RuleFor(x => x.CLIENT_SECRET).NotEmpty().WithMessage("Slack CLIENT_SECRET is required");
        RuleFor(x => x.CLIENT_SIGNING_SECRET).NotEmpty().WithMessage("Slack CLIENT_SIGNING_SECRET is required");
    }
}
