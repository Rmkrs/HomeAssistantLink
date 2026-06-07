namespace HomeAssistantLink.Plugins.ShutDownComputerUnitTests;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Plugins.ShutDownComputer;
using HomeAssistantLink.Plugins.ShutDownComputer.Contracts;
using Library.UnitTesting.Common;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class ShutDownPluginTests : UnitTestBase<ShutDownPlugin>
{
    private readonly Mock<IOptions<ShutdownPluginConfig>> optionsMock = new();
    private readonly Mock<IShutdownInvoker> shutdownInvokerMock = new();
    private readonly ShutdownPluginConfig shutdownPluginConfig = new();

    [Test]
    public void Execute_Valid_ShutsDown()
    {
        // Arrange
        var entityId = this.Instantiator.Random<String>();
        var state = this.Instantiator.Random<String>();

        this.shutdownPluginConfig.EntityId = entityId;
        this.shutdownPluginConfig.Command = state;

        var target = this.GetTarget();

        // Act
        target.Execute(entityId, state);

        // Assert
        this.shutdownInvokerMock.Verify(m => m.Invoke(), Times.Once, "ShutdownInvoker should be invoked once.");
    }

    [Test]
    public void Execute_WithDifferentEntityId_DoesNotShutDown()
    {
        // Arrange
        var entityId = this.Instantiator.Random<String>();
        var state = this.Instantiator.Random<String>();

        this.shutdownPluginConfig.EntityId = this.Instantiator.Random<string>();
        this.shutdownPluginConfig.Command = state;

        var target = this.GetTarget();

        // Act
        target.Execute(entityId, state);

        // Assert
        this.shutdownInvokerMock.Verify(m => m.Invoke(), Times.Never, "ShutdownInvoker should not be invoked.");
    }

    [Test]
    public void Execute_WithDifferentCommand_DoesNotShutDown()
    {
        // Arrange
        var entityId = this.Instantiator.Random<String>();
        var state = this.Instantiator.Random<String>();

        this.shutdownPluginConfig.EntityId = entityId;
        this.shutdownPluginConfig.Command = this.Instantiator.Random<string>();

        var target = this.GetTarget();

        // Act
        target.Execute(entityId, state);

        // Assert
        this.shutdownInvokerMock.Verify(m => m.Invoke(), Times.Never, "ShutdownInvoker should not be invoked.");
    }

    [Test]
    public void CanExecute_Valid_ReturnsTrue()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var state = this.Instantiator.Random<string>();

        this.shutdownPluginConfig.EntityId = entityId;
        this.shutdownPluginConfig.Command = state;

        var target = this.GetTarget();

        // Act
        var actual = target.CanExecute(entityId, state);

        // Assert
        actual.ShouldBeTrue();
    }

    protected override void Setup()
    {
        this.optionsMock.Reset();
        this.shutdownInvokerMock.Reset();

        this.shutdownPluginConfig.RunAs = PluginRunAs.System;
        this.shutdownPluginConfig.EntityId = this.Instantiator.Random<string>();
        this.shutdownPluginConfig.Command = this.Instantiator.Random<string>();

        this.optionsMock.Setup(o => o.Value).Returns(this.shutdownPluginConfig);
    }

    protected override ShutDownPlugin GetTarget()
    {
        return new ShutDownPlugin(this.optionsMock.Object, this.shutdownInvokerMock.Object);
    }
}
