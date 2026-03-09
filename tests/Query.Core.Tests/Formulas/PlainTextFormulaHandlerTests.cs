using FluentAssertions;
using Query.Core.Formulas;

namespace Query.Core.Tests.Formulas;

public class PlainTextFormulaHandlerTests
{
    private readonly PlainTextFormulaHandler _handler = new();

    [Theory]
    [InlineData("revenue minus cost")]
    [InlineData("revenue divided by total")]
    [InlineData("revenue minus cost divided by revenue")]
    public void CanHandle_PlainTextArithmetic_ReturnsTrue(string input)
    {
        _handler.CanHandle(input).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_NormalQuery_ReturnsFalse()
    {
        _handler.CanHandle("show revenue by region for 2024").Should().BeFalse();
    }

    [Fact]
    public void Parse_RevenueMinusCost_ProducesBinaryOp()
    {
        var ast = _handler.Parse("revenue minus cost");
        ast.ToSql().Should().Be("(revenue - cost)");
    }

    [Fact]
    public void Parse_DividedBy_ProducesDivision()
    {
        var ast = _handler.Parse("gross divided by total");
        ast.ToSql().Should().Be("(gross / total)");
    }
}
