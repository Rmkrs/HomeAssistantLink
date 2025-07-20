namespace HomeAssistantLink.Domain.Contracts;

public interface IMonitorDateTime : IMonitor
{
    DateTime Value { get; }
}