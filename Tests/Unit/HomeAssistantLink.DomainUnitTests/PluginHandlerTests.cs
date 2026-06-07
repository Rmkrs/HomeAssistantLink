namespace HomeAssistantLink.DomainUnitTests;

using HomeAssistantLink.Domain;
using HomeAssistantLink.Domain.Contracts;
using Library.UnitTesting.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

[TestFixture]
public class PluginHandlerTests : UnitTestBase<PluginHandler>
{
    private readonly Mock<IPlugin> pluginMock = new();
    private readonly Mock<IUserPluginCommandClient> userPluginCommandClientMock = new();
    private readonly Mock<IOptions<PluginHostConfig>> optionsMock = new();
    private readonly Mock<ILogger<PluginHandler>> loggerMock = new();
    private readonly PluginHostConfig pluginHostConfig = new();
    private readonly Mock<IPluginCommandCatalog> pluginCommandCatalogMock = new();

    [Test]
    public void Handle_SystemPluginOnSystemHost_ExecutesPlugin()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var state = this.Instantiator.Random<string>();

        this.pluginHostConfig.RunAs = PluginRunAs.System;
        this.SetupPluginCommand(entityId, state, PluginRunAs.System);

        var target = this.GetTarget();

        // Act
        target.Handle(entityId, state);

        // Assert
        this.pluginMock.Verify(
            p => p.Execute(entityId, state),
            Times.Once,
            "System plugin should execute on the system host.");

        this.userPluginCommandClientMock.Verify(
            p => p.Handle(It.IsAny<PluginCommand>()),
            Times.Never,
            "User plugin client should not be called for system plugins on the system host.");
    }

    [Test]
    public void Handle_UserPluginOnSystemHost_ForwardsCommand()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var state = this.Instantiator.Random<string>();

        this.pluginHostConfig.RunAs = PluginRunAs.System;
        this.SetupPluginCommand(entityId, state, PluginRunAs.User);

        var target = this.GetTarget();

        // Act
        target.Handle(entityId, state);

        // Assert
        this.pluginMock.Verify(
            p => p.Execute(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "User plugin should not execute inside the system host.");

        this.userPluginCommandClientMock.Verify(
            p => p.Handle(It.Is<PluginCommand>(command =>
                command.PluginType == "TestPlugin" &&
                command.DisplayName == "Test Command" &&
                command.EntityId == entityId &&
                command.State == state &&
                command.RunAs == PluginRunAs.User)),
            Times.Once,
            "User plugin command should be forwarded.");
    }

    [Test]
    public void Handle_UserPluginOnUserHost_ExecutesPlugin()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var state = this.Instantiator.Random<string>();

        this.pluginHostConfig.RunAs = PluginRunAs.User;
        this.SetupPluginCommand(entityId, state, PluginRunAs.User);

        var target = this.GetTarget();

        // Act
        target.Handle(entityId, state);

        // Assert
        this.pluginMock.Verify(
            p => p.Execute(entityId, state),
            Times.Once,
            "User plugin should execute on the user host.");

        this.userPluginCommandClientMock.Verify(
            p => p.Handle(It.IsAny<PluginCommand>()),
            Times.Never,
            "User plugin client should not be called when executing directly on the user host.");
    }

    [Test]
    public void Handle_SystemPluginOnUserHost_IgnoresCommand()
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var state = this.Instantiator.Random<string>();

        this.pluginHostConfig.RunAs = PluginRunAs.User;
        this.SetupPluginCommand(entityId, state, PluginRunAs.System);

        var target = this.GetTarget();

        // Act
        target.Handle(entityId, state);

        // Assert
        this.pluginMock.Verify(
            p => p.Execute(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "System plugin should not execute on the user host.");

        this.userPluginCommandClientMock.Verify(
            p => p.Handle(It.IsAny<PluginCommand>()),
            Times.Never,
            "User plugin client should not be called from the user host for system commands.");
    }

    protected override void Setup()
    {
        this.pluginMock.Reset();
        this.userPluginCommandClientMock.Reset();
        this.optionsMock.Reset();
        this.loggerMock.Reset();
        this.pluginCommandCatalogMock.Reset();

        this.pluginHostConfig.RunAs = PluginRunAs.System;

        this.optionsMock
            .Setup(o => o.Value)
            .Returns(this.pluginHostConfig);
    }

    protected override PluginHandler GetTarget()
    {
        return new PluginHandler(
            [this.pluginMock.Object],
            this.pluginCommandCatalogMock.Object,
            this.userPluginCommandClientMock.Object,
            this.optionsMock.Object,
            this.loggerMock.Object);
    }

    private void SetupPluginCommand(
        string entityId,
        string state,
        PluginRunAs runAs)
    {
        var commandId = PluginCommandId.Create("TestPlugin", entityId, state);

        var command = new PluginCommand(
            CommandId: commandId,
            PluginType: "TestPlugin",
            DisplayName: "Test Command",
            EntityId: entityId,
            State: state,
            RunAs: runAs,
            Parameters: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        this.pluginMock
            .Setup(p => p.CanExecute(entityId, state))
            .Returns(true);

        this.pluginMock
            .Setup(p => p.GetCommands())
            .Returns([command]);

        this.pluginCommandCatalogMock
            .Setup(c => c.GetCommand(entityId, state))
            .Returns(command);
    }
}
