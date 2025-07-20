namespace HomeAssistantLink.InfrastructureUnitTests;

using FluentAssertions;
using HomeAssistantLink.Infrastructure;
using Library.UnitTesting.Common;

[TestFixture]
public class DebounceTests : UnitTestBase<Debounce>
{
    [Test]
    public void ShouldProcess_Valid_ReturnsCorrectResult()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var value = this.Instantiator.Random<string>();

        var target = this.GetTarget();

        // Act
        var actual = target.ShouldProcess(entityId, value);

        // Assert
        actual.Should().BeTrue();
    }

    [Test]
    public void ShouldProcess_SameValueTooFast_ReturnsCorrectResult()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var value = this.Instantiator.Random<string>();

        var target = this.GetTarget();

        // Act
        target.ShouldProcess(entityId, value);
        var actual = target.ShouldProcess(entityId, value);

        // Assert
        actual.Should().BeFalse();
    }

    [Test]
    public void ShouldProcess_SameValueWithProperDelay_ReturnsCorrectResult()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var value = this.Instantiator.Random<string>();

        var target = this.GetTarget();

        // Act
        target.ShouldProcess(entityId, value);
        Thread.Sleep(2000);
        var actual = target.ShouldProcess(entityId, value);

        // Assert
        actual.Should().BeTrue();
    }

    [Test]
    public void ShouldProcess_Different_ReturnsCorrectResult()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var value = this.Instantiator.Random<string>();
        var otherValue = this.Instantiator.Random<string>();

        var target = this.GetTarget();

        // Act
        target.ShouldProcess(entityId, value);
        var actual = target.ShouldProcess(entityId, otherValue);

        // Assert
        actual.Should().BeTrue();
    }

    protected override Debounce GetTarget()
    {
        return new Debounce();
    }
}