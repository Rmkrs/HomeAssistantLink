namespace HomeAssistantLink.DomainUnitTests;

using HomeAssistantLink.Domain;
using HomeAssistantLink.Domain.Contracts;
using Library.UnitTesting.Common;
using Moq;
using NUnit.Framework;

[TestFixture]
public class MonitorHandlerTests : UnitTestBase<MonitorHandler>
{
    private readonly Mock<ISetEntityState> setEntityStateMock = new();
    private readonly Mock<IDebounce> debounceMock = new();
    private readonly Mock<IMonitor> monitorMock = new();

    [Test]
    public async Task StartAsync_Valid_StartsMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        await target.StartAsync(CancellationToken.None);

        // Assert
        this.monitorMock.Verify(
            m => m.StartAsync(
                It.IsAny<Func<EntityStateUpdate, CancellationToken, Task>>(),
                CancellationToken.None),
            Times.Once,
            "Monitor should be started once.");
    }

    [Test]
    public async Task StopAsync_Valid_StopsMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        await target.StopAsync(CancellationToken.None);

        // Assert
        this.monitorMock.Verify(
            m => m.StopAsync(CancellationToken.None),
            Times.Once,
            "Monitor should be stopped once.");
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task MonitorChange_Valid_SetsEntityStateWhenApplicable(bool isApplicable)
    {
        // Arrange
        var update = new EntityStateUpdate(
            this.Instantiator.Random<string>(),
            HomeAssistantEntityType.Text,
            this.Instantiator.Random<string>());

        this.debounceMock
            .Setup(d => d.ShouldProcess(update))
            .Returns(isApplicable);

        Func<EntityStateUpdate, CancellationToken, Task>? capturedPublish = null;

        this.monitorMock
            .Setup(m => m.StartAsync(
                It.IsAny<Func<EntityStateUpdate, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Func<EntityStateUpdate, CancellationToken, Task>, CancellationToken>((publish, _) => capturedPublish = publish)
            .Returns(Task.CompletedTask);

        this.setEntityStateMock
            .Setup(m => m.SetAsync(update, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var target = this.GetTarget();
        await target.StartAsync(CancellationToken.None);

        // Act
        await capturedPublish!(update, CancellationToken.None);

        // Assert
        this.setEntityStateMock.Verify(
            m => m.SetAsync(update, CancellationToken.None),
            isApplicable ? Times.Once : Times.Never,
            "SetEntityState should be called with the correct update when applicable.");
    }

    protected override void Setup()
    {
        this.setEntityStateMock.Reset();
        this.debounceMock.Reset();
        this.monitorMock.Reset();

        this.monitorMock
            .Setup(m => m.StartAsync(
                It.IsAny<Func<EntityStateUpdate, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        this.monitorMock
            .Setup(m => m.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    protected override MonitorHandler GetTarget()
    {
        return new MonitorHandler(
            this.setEntityStateMock.Object,
            this.debounceMock.Object,
            [this.monitorMock.Object]);
    }
}