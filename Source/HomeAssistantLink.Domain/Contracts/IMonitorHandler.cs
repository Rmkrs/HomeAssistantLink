namespace HomeAssistantLink.Domain.Contracts;

public interface IMonitorHandler
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}