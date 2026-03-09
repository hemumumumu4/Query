using FluentAssertions;
using Moq;
using Query.Core.Formulas;
using Query.Core.Schema;

namespace Query.Core.Tests.Formulas;

public class FormulaHandlerRegistryTests
{
    [Fact]
    public void Registry_DetectsRegisteredHandler()
    {
        var handler = new Mock<IFormulaHandler>();
        handler.Setup(h => h.CanHandle("(a + b)")).Returns(true);
        handler.Setup(h => h.HandlerName).Returns("infix");

        var registry = new FormulaHandlerRegistry();
        registry.Register(handler.Object);

        registry.Detect("(a + b)").Should().NotBeNull();
        registry.Detect("(a + b)")!.HandlerName.Should().Be("infix");
    }

    [Fact]
    public void Registry_ReturnsNull_WhenNoHandlerMatches()
    {
        var registry = new FormulaHandlerRegistry();
        registry.Detect("plain text query").Should().BeNull();
    }
}
