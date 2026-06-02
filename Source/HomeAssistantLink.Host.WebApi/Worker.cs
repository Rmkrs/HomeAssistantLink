namespace HomeAssistantLink.Host.WebApi;

using HomeAssistantLink.Domain.Contracts;

public class Worker(IMonitorHandler handler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await handler.StartAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            await handler.StopAsync(CancellationToken.None);
        }
    }
}