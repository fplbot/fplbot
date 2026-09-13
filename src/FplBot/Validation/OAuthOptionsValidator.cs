using FluentValidation;
using Slackbot.Net.Endpoints.Hosting;

namespace FplBot.Config;

public class OAuthOptionsValidator : AbstractValidator<OAuthOptions>
{
    public OAuthOptionsValidator()
    {
        RuleFor(x => x.CLIENT_ID).NotEmpty().WithMessage("Slack CLIENT_ID is required");
        RuleFor(x => x.CLIENT_SECRET).NotEmpty().WithMessage("Slack CLIENT_SECRET is required");
    }
}
