using FluentAssertions;

namespace Deck.Services.Tests;

/// <summary>
/// 开发环境配置验证测试
/// </summary>
public class DevEnvironmentTests
{
    [Fact]
    public void FluentAssertions_ShouldWork()
    {
        // Arrange
        var expected = "Hello World";
        
        // Act & Assert
        expected.Should().NotBeNull();
        expected.Should().Be("Hello World");
        expected.Should().StartWith("Hello");
        expected.Should().EndWith("World");
        expected.Should().HaveLength(11);
    }

    [Fact]
    public void Collections_ShouldWork_WithFluentAssertions()
    {
        // Arrange
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        
        // Act & Assert
        numbers.Should().NotBeEmpty();
        numbers.Should().HaveCount(5);
        numbers.Should().Contain(3);
        numbers.Should().BeInAscendingOrder();
        numbers.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Exceptions_ShouldWork_WithFluentAssertions()
    {
        // Arrange & Act
        Action act = () => throw new ArgumentException("Test exception", nameof(act));
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Test exception*")
            .And.ParamName.Should().Be(nameof(act));
    }
}