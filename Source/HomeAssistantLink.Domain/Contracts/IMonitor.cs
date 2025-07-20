namespace HomeAssistantLink.Domain.Contracts;

public interface IMonitor
{
    string EntityId { get; }

    void Start(Action action);

    void Stop();
}