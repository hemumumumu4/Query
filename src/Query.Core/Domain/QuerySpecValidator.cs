using FluentValidation;

namespace Query.Core.Domain;

public class QuerySpecValidator : AbstractValidator<QuerySpec>
{
    private static readonly string[] ValidFormats = ["sql", "markdown", "html"];

    public QuerySpecValidator()
    {
        RuleFor(x => x.Intent).NotEmpty().WithMessage("Intent is required");
        RuleFor(x => x.Entities).NotEmpty().WithMessage("At least one entity is required");
        RuleFor(x => x.OutputFormat)
            .Must(f => ValidFormats.Contains(f))
            .WithMessage($"OutputFormat must be one of: {string.Join(", ", ValidFormats)}");
    }
}
