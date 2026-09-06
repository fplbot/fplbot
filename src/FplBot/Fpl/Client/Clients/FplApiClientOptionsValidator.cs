using FluentValidation;

namespace Fpl.Client.Clients;

public class FplApiClientOptionsValidator : AbstractValidator<FplApiClientOptions>
{
    public FplApiClientOptionsValidator()
    {
        RuleFor(x => x.Login).NotEmpty().WithMessage("fpl:Login is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("fpl:Password is required");
    }
}
