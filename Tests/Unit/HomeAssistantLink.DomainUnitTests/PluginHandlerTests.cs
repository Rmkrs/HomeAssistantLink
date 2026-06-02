namespace HomeAssistantLink.DomainUnitTests;

using HomeAssistantLink.Domain;
using HomeAssistantLink.Domain.Contracts;
using Library.UnitTesting.Common;
using Moq;
using NUnit.Framework;

[TestFixture]
public class PluginHandlerTests : UnitTestBase<PluginHandler>
{
    private readonly Mock<IPlugin> pluginMock = new();

    [Test]
    public void Handle_Valid_ExecutesPlugin()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var state = this.Instantiator.Random<string>();
        var target = this.GetTarget();

        // Act
        target.Handle(entityId, state);

        // Assert
        this.pluginMock.Verify(
            p => p.Execute(entityId, state),
            Times.Once,
            "Plugin should execute with the provided entityId and state."
        );
    }

    protected override void Setup()
    {
        this.pluginMock.Reset();
    }

    protected override PluginHandler GetTarget()
    {
        return new PluginHandler([this.pluginMock.Object]);
    }
}
