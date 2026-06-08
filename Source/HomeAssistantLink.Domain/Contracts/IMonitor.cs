namespace HomeAssistantLink.Domain.Contracts;

public interface IMonitor
{
    string Name { get; }

    Task StartAsync(Func<EntityStateUpdate, CancellationToken, Task> publish, CancellationToken ct);

    Task StopAsync(CancellationToken ct);
}
