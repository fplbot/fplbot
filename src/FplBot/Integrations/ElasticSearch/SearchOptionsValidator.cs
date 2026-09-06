using FluentValidation;

namespace Fpl.Search;

public class SearchOptionsValidator : AbstractValidator<SearchOptions>
{
    public SearchOptionsValidator()
    {
        RuleFor(x => x.IndexUri).NotEmpty();
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.EntriesIndex).NotEmpty();
        RuleFor(x => x.LeaguesIndex).NotEmpty();
        RuleFor(x => x.AnalyticsIndex).NotEmpty();
        RuleFor(x => x.IndexingCron).NotEmpty()
            .When(x => x.ShouldIndexEntries || x.ShouldIndexLeagues)
            .WithMessage("IndexingCron is required when indexing is enabled");
        RuleFor(x => x.ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob).GreaterThan(0)
            .When(x => x.ShouldIndexLeagues)
            .WithMessage("ConsecutiveCountOfMissingLeaguesBeforeStoppingIndexJob must be > 0 when ShouldIndexLeagues is true");
    }
}
