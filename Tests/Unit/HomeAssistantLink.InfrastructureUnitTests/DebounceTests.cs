namespace HomeAssistantLink.InfrastructureUnitTests;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Infrastructure;
using Library.UnitTesting.Common;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class DebounceTests : UnitTestBase<Debounce>
{
    private readonly TestClock clock = new();

    [Test]
    public void ShouldProcess_Valid_ReturnsCorrectResult()
    {
        // Arrange
        var update = this.CreateUpdate();

        var target = this.GetTarget();

        // Act
        var actual = target.ShouldProcess(update);

        // Assert
        actual.ShouldBeTrue();
    }

    [Test]
    public void ShouldProcess_SameValueTooFast_ReturnsCorrectResult()
    {
        // Arrange
        var update = this.CreateUpdate();

        var target = this.GetTarget();

        // Act
        target.ShouldProcess(update);
        var actual = target.ShouldProcess(update);

        // Assert
        actual.ShouldBeFalse();
    }

    [Test]
    public void ShouldProcess_SameValueWithProperDelay_ReturnsCorrectResult()
    {
        // Arrange
        var update = this.CreateUpdate();

        var target = this.GetTarget();

        // Act
        target.ShouldProcess(update);
        this.clock.Advance(TimeSpan.FromSeconds(2));
        var actual = target.ShouldProcess(update);

        // Assert
        actual.ShouldBeTrue();
    }

    [Test]
    public void ShouldProcess_DifferentValue_ReturnsCorrectResult()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var update = this.CreateUpdate(entityId, this.Instantiator.Random<string>());
        var otherUpdate = this.CreateUpdate(entityId, this.Instantiator.Random<string>());

        var target = this.GetTarget();

        // Act
        target.ShouldProcess(update);
        var actual = target.ShouldProcess(otherUpdate);

        // Assert
        actual.ShouldBeTrue();
    }

    protected override void Setup()
    {
        this.clock.Reset();
    }

    protected override Debounce GetTarget()
    {
        return new Debounce(this.clock);
    }

    private EntityStateUpdate CreateUpdate()
    {
        return this.CreateUpdate(
            this.Instantiator.Random<string>(),
            this.Instantiator.Random<string>());
    }

    private EntityStateUpdate CreateUpdate(string entityId, string value)
    {
        return new EntityStateUpdate(
            entityId,
            HomeAssistantEntityType.Text,
            value);
    }

    private sealed class TestClock : IClock
    {
        private static readonly DateTime startTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime UtcNow { get; private set; } = startTime;

        public void Advance(TimeSpan timeSpan)
        {
            this.UtcNow = this.UtcNow.Add(timeSpan);
        }

        public void Reset()
        {
            this.UtcNow = startTime;
        }
    }
}
