namespace HomeAssistantLink.Domain.Contracts;

public interface IMonitorBool : IMonitor
{
    bool Value { get; }
}