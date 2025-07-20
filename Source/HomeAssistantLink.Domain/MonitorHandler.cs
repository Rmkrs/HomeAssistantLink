namespace HomeAssistantLink.Domain;

using HomeAssistantLink.Domain.Contracts;

public class MonitorHandler(
    ISetEntityState setEntityState,
    IDebounce debounce,
    IEnumerable<IMonitorBool> boolMonitors,
    IEnumerable<IMonitorDateTime> dateMonitors,
    IEnumerable<IMonitorDouble> doubleMonitors,
    IEnumerable<IMonitorString> stringMonitors) : IMonitorHandler
{
    private readonly ISetEntityState setEntityState = setEntityState ?? throw new ArgumentNullException(nameof(setEntityState));
    private readonly IDebounce debounce = debounce ?? throw new ArgumentNullException(nameof(debounce));
    private readonly IEnumerable<IMonitorBool> boolMonitors = boolMonitors ?? throw new ArgumentNullException(nameof(boolMonitors));
    private readonly IEnumerable<IMonitorDateTime> dateMonitors = dateMonitors ?? throw new ArgumentNullException(nameof(dateMonitors));
    private readonly IEnumerable<IMonitorDouble> doubleMonitors = doubleMonitors ?? throw new ArgumentNullException(nameof(doubleMonitors));
    private readonly IEnumerable<IMonitorString> stringMonitors = stringMonitors ?? throw new ArgumentNullException(nameof(stringMonitors));

    public void Start()
    {
        foreach (var monitor in this.boolMonitors)
        {
            monitor.Start(() => this.HandleMonitorChange(monitor));
        }

        foreach (var monitor in this.dateMonitors)
        {
            monitor.Start(() => this.HandleMonitorChange(monitor));
        }

        foreach (var monitor in this.doubleMonitors)
        {
            monitor.Start(() => this.HandleMonitorChange(monitor));
        }

        foreach (var monitor in this.stringMonitors)
        {
            monitor.Start(() => this.HandleMonitorChange(monitor));
        }
    }

    public void Stop()
    {
        foreach (var monitor in this.boolMonitors)
        {
            monitor.Stop();
        }

        foreach (var monitor in this.dateMonitors)
        {
            monitor.Stop();
        }

        foreach (var monitor in this.doubleMonitors)
        {
            monitor.Stop();
        }

        foreach (var monitor in this.stringMonitors)
        {
            monitor.Stop();
        }
    }

    private void HandleMonitorChange(IMonitorBool monitor)
    {
        if (!this.debounce.ShouldProcess(monitor.EntityId, monitor.Value))
        {
            return;
        }

        this.setEntityState.SetBool(monitor.EntityId, monitor.Value);
    }

    private void HandleMonitorChange(IMonitorDateTime monitor)
    {
        if (!this.debounce.ShouldProcess(monitor.EntityId, monitor.Value))
        {
            return;
        }

        this.setEntityState.SetDate(monitor.EntityId, monitor.Value);
    }

    private void HandleMonitorChange(IMonitorDouble monitor)
    {
        if (!this.debounce.ShouldProcess(monitor.EntityId, monitor.Value))
        {
            return;
        }

        this.setEntityState.SetNumber(monitor.EntityId, monitor.Value);
    }

    private void HandleMonitorChange(IMonitorString monitor)
    {
        if (!this.debounce.ShouldProcess(monitor.EntityId, monitor.Value))
        {
            return;
        }

        this.setEntityState.SetString(monitor.EntityId, monitor.Value);
    }
}
