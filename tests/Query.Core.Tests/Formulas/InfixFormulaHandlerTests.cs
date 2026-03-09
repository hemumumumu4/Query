using FluentAssertions;
using Query.Core.Formulas;

namespace Query.Core.Tests.Formulas;

public class InfixFormulaHandlerTests
{
    private readonly InfixFormulaHandler _handler = new();

    [Theory]
    [InlineData("(a + b)")]
    [InlineData("revenue - cost")]
    [InlineData("(revenue - cost) / revenue * 100")]
    public void CanHandle_InfixExpression_ReturnsTrue(string input)
    {
        _handler.CanHandle(input).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_PlainSentence_ReturnsFalse()
    {
        _handler.CanHandle("show me revenue by region").Should().BeFalse();
    }

    [Fact]
    public void Parse_SimpleAddition_ProducesBinaryOp()
    {
        var ast = _handler.Parse("revenue - cost");
        ast.ToSql().Should().Be("(revenue - cost)");
    }

    [Fact]
    public void Parse_ComplexExpression_ProducesNestedOps()
    {
        var ast = _handler.Parse("(revenue - cost) / revenue");
        ast.ToSql().Should().Be("((revenue - cost) / revenue)");
    }
}
