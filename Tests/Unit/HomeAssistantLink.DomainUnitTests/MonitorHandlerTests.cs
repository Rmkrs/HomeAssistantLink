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
    private readonly Mock<IMonitorBool> boolMonitorMock = new();
    private readonly Mock<IMonitorDateTime> dateMonitorMock = new();
    private readonly Mock<IMonitorDouble> doubleMonitorMock = new();
    private readonly Mock<IMonitorString> stringMonitorMock = new();

    [Test]
    public void Start_Valid_StartsBoolMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        target.Start();

        // Assert
        this.boolMonitorMock.Verify(m => m.Start(It.IsAny<Action>()), Times.Once, "Bool monitor should be started once.");
    }

    [Test]
    public void Start_Valid_StartsDateMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        target.Start();

        // Assert
        this.dateMonitorMock.Verify(m => m.Start(It.IsAny<Action>()), Times.Once, "Date monitor should be started once.");
    }

    [Test]
    public void Start_Valid_StartsDoubleMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        target.Start();

        // Assert
        this.doubleMonitorMock.Verify(m => m.Start(It.IsAny<Action>()), Times.Once, "Double monitor should be started once.");
    }

    [Test]
    public void Start_Valid_StartsStringMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        target.Start();

        // Assert
        this.stringMonitorMock.Verify(m => m.Start(It.IsAny<Action>()), Times.Once, "String monitor should be started once.");
    }

    [Test]
    public void Stop_Valid_StopsBoolMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        target.Stop();

        // Assert
        this.boolMonitorMock.Verify(m => m.Stop(), Times.Once, "Bool monitor should be stopped once.");
    }

    [Test]
    public void Stop_Valid_StopsDateMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        target.Stop();

        // Assert
        this.dateMonitorMock.Verify(m => m.Stop(), Times.Once, "Date monitor should be stopped once.");
    }

    [Test]
    public void Stop_Valid_StopsDoubleMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        target.Stop();

        // Assert
        this.doubleMonitorMock.Verify(m => m.Stop(), Times.Once, "Double monitor should be stopped once.");
    }

    [Test]
    public void Stop_Valid_StopsStringMonitor()
    {
        // Arrange
        var target = this.GetTarget();

        // Act
        target.Stop();

        // Assert
        this.stringMonitorMock.Verify(m => m.Stop(), Times.Once, "String monitor should be stopped once.");
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void BoolMonitorChange_Valid_SetsBoolWhenApplicable(bool isApplicable)
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var value = this.Instantiator.Random<bool>();
        this.boolMonitorMock.Setup(m => m.EntityId).Returns(entityId);
        this.boolMonitorMock.Setup(m => m.Value).Returns(value);
        this.debounceMock.Setup(d => d.ShouldProcess(entityId, value)).Returns(isApplicable);

        Action? capturedAction = null;

        this.boolMonitorMock
            .Setup(m => m.Start(It.IsAny<Action>()))
            .Callback<Action>(a => capturedAction = a);

        var target = this.GetTarget();
        target.Start();

        // Act
        capturedAction?.Invoke();

        // Assert
        this.setEntityStateMock.Verify(
            m => m.SetBool(entityId, value),
            isApplicable ? Times.Once : Times.Never,
            "SetEntityState should be called with the correct parameters when applicable.");
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void DateMonitorChange_Valid_SetsDateWhenApplicable(bool isApplicable)
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var value = this.Instantiator.Random<DateTime>();
        this.dateMonitorMock.Setup(m => m.EntityId).Returns(entityId);
        this.dateMonitorMock.Setup(m => m.Value).Returns(value);
        this.debounceMock.Setup(d => d.ShouldProcess(entityId, value)).Returns(isApplicable);

        Action? capturedAction = null;

        this.dateMonitorMock
            .Setup(m => m.Start(It.IsAny<Action>()))
            .Callback<Action>(a => capturedAction = a);

        var target = this.GetTarget();
        target.Start();

        // Act
        capturedAction?.Invoke();

        // Assert
        this.setEntityStateMock.Verify(
            m => m.SetDate(entityId, value),
            isApplicable ? Times.Once : Times.Never,
            "SetEntityState should be called with the correct parameters when applicable.");
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void DoubleMonitorChange_Valid_SetsNumberWhenApplicable(bool isApplicable)
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var value = this.Instantiator.Random<Double>();
        this.doubleMonitorMock.Setup(m => m.EntityId).Returns(entityId);
        this.doubleMonitorMock.Setup(m => m.Value).Returns(value);
        this.debounceMock.Setup(d => d.ShouldProcess(entityId, value)).Returns(isApplicable);

        Action? capturedAction = null;

        this.doubleMonitorMock
            .Setup(m => m.Start(It.IsAny<Action>()))
            .Callback<Action>(a => capturedAction = a);

        var target = this.GetTarget();
        target.Start();

        // Act
        capturedAction?.Invoke();

        // Assert
        this.setEntityStateMock.Verify(
            m => m.SetNumber(entityId, value),
            isApplicable ? Times.Once : Times.Never,
            "SetEntityState should be called with the correct parameters when applicable.");
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void StringMonitorChange_Valid_SetsStringWhenApplicable(bool isApplicable)
    {
        // Arrange
        var entityId = this.Instantiator.Random<string>();
        var value = this.Instantiator.Random<String>();
        this.stringMonitorMock.Setup(m => m.EntityId).Returns(entityId);
        this.stringMonitorMock.Setup(m => m.Value).Returns(value);
        this.debounceMock.Setup(d => d.ShouldProcess(entityId, value)).Returns(isApplicable);

        Action? capturedAction = null;

        this.stringMonitorMock
            .Setup(m => m.Start(It.IsAny<Action>()))
            .Callback<Action>(a => capturedAction = a);

        var target = this.GetTarget();
        target.Start();

        // Act
        capturedAction?.Invoke();

        // Assert
        this.setEntityStateMock.Verify(
            m => m.SetString(entityId, value),
            isApplicable ? Times.Once : Times.Never,
            "SetEntityState should be called with the correct parameters when applicable.");
    }

    protected override void Setup()
    {
        this.setEntityStateMock.Reset();
        this.debounceMock.Reset();
        this.boolMonitorMock.Reset();
        this.dateMonitorMock.Reset();
        this.doubleMonitorMock.Reset();
        this.stringMonitorMock.Reset();
    }

    protected override MonitorHandler GetTarget()
    {
        return new MonitorHandler(
            this.setEntityStateMock.Object,
            this.debounceMock.Object,
            [this.boolMonitorMock.Object],
            [this.dateMonitorMock.Object],
            [this.doubleMonitorMock.Object],
            [this.stringMonitorMock.Object]);
    }
}