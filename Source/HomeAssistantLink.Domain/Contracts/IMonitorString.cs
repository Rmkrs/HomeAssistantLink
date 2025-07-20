namespace HomeAssistantLink.Domain.Contracts;

public interface IMonitorString : IMonitor
{
    string Value { get; }
}