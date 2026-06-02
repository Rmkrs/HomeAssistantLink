namespace HomeAssistantLink.Domain;

using HomeAssistantLink.Domain.Contracts;

public class MonitorHandler(
    ISetEntityState setEntityState,
    IDebounce debounce,
    IEnumerable<IMonitor> monitors) : IMonitorHandler
{
    private readonly ISetEntityState setEntityState = setEntityState ?? throw new ArgumentNullException(nameof(setEntityState));
    private readonly IDebounce debounce = debounce ?? throw new ArgumentNullException(nameof(debounce));
    private readonly IEnumerable<IMonitor> monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var monitor in this.monitors)
        {
            await monitor.StartAsync(this.HandleMonitorUpdateAsync, cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var monitor in this.monitors)
        {
            await monitor.StopAsync(cancellationToken);
        }
    }

    private async Task HandleMonitorUpdateAsync(EntityStateUpdate update, CancellationToken cancellationToken)
    {
        if (!this.debounce.ShouldProcess(update))
        {
            return;
        }

        await this.setEntityState.SetAsync(update, cancellationToken);
    }
}