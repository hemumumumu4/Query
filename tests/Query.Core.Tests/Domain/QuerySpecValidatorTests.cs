using FluentAssertions;
using FluentValidation;
using Query.Core.Domain;

namespace Query.Core.Tests.Domain;

public class QuerySpecValidatorTests
{
    private readonly QuerySpecValidator _validator = new();

    [Fact]
    public void Valid_Spec_PassesValidation()
    {
        var spec = new QuerySpec
        {
            Intent = "aggregation",
            Entities = [new EntityRef("orders", "o")],
            Measures = [new MeasureDef("SUM(o.revenue)", "total_revenue")]
        };
        _validator.Validate(spec).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Intent_FailsValidation()
    {
        var spec = new QuerySpec { Intent = "" };
        var result = _validator.Validate(spec);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Intent");
    }

    [Fact]
    public void No_Entities_FailsValidation()
    {
        var spec = new QuerySpec { Intent = "aggregation", Entities = [] };
        var result = _validator.Validate(spec);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Entities");
    }

    [Fact]
    public void Invalid_OutputFormat_FailsValidation()
    {
        var spec = new QuerySpec
        {
            Intent = "aggregation",
            Entities = [new EntityRef("orders", "o")],
            OutputFormat = "invalid"
        };
        var result = _validator.Validate(spec);
        result.IsValid.Should().BeFalse();
    }
}
