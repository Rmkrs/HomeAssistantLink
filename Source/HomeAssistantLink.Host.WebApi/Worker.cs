namespace HomeAssistantLink.Host.WebApi;

using HomeAssistantLink.Domain.Contracts;

public class Worker(IMonitorHandler handler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            handler.Start();
            await Task.Delay(Timeout.Infinite, stoppingToken);
            handler.Stop();
        }
    }
}