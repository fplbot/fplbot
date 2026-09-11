using FluentValidation;
using Microsoft.Extensions.Options;

namespace FplBot.Config;

public class FluentValidationOptionsValidator<T>(IValidator<T> validator) : IValidateOptions<T> where T : class
{
    public ValidateOptionsResult Validate(string? name, T options)
    {
        var result = validator.Validate(options);
        return result.IsValid ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(result.Errors.Select(e => e.ErrorMessage));
    }
}

public static class OptionsBuilderFluentValidationExtensions
{
    public static OptionsBuilder<T> ValidateWithFluentValidation<T>(
        this OptionsBuilder<T> builder,
        AbstractValidator<T> validator) where T : class
    {
        builder.Services.AddSingleton<IValidateOptions<T>>(new FluentValidationOptionsValidator<T>(validator));
        return builder;
    }
}
