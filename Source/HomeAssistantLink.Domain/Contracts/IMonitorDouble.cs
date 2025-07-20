namespace HomeAssistantLink.Domain.Contracts;

public interface IMonitorDouble : IMonitor
{
    double Value { get; }
}