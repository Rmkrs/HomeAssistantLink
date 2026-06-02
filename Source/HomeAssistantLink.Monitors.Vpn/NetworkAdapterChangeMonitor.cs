namespace HomeAssistantLink.Monitors.Vpn;

using System.Management;
using HomeAssistantLink.Monitors.Vpn.Contracts;

public sealed class NetworkAdapterChangeMonitor : INetworkAdapterChangeMonitor, IDisposable
{
    private readonly ManagementEventWatcher watcher = new();
    private Action? actionToInvokeWhenEventArrives;
    private bool disposed;

    public NetworkAdapterChangeMonitor()
    {
        var networkMonitorQuery = new EventQuery("Select * From __InstanceModificationEvent Within 1 where TargetInstance ISA 'Win32_NetworkAdapter'");

        this.watcher.Query = networkMonitorQuery;
        this.watcher.EventArrived += this.EventArrived;
    }

    public void Start(Action action)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        ArgumentNullException.ThrowIfNull(action);

        this.actionToInvokeWhenEventArrives = action;
        this.watcher.Start();
    }

    public void StopMonitoring()
    {
        if (this.disposed)
        {
            return;
        }

        this.watcher.Stop();
        this.actionToInvokeWhenEventArrives = null;
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.watcher.EventArrived -= this.EventArrived;
        this.watcher.Stop();
        this.watcher.Dispose();

        this.actionToInvokeWhenEventArrives = null;
        this.disposed = true;

        GC.SuppressFinalize(this);
    }

    private void EventArrived(object sender, EventArrivedEventArgs e)
    {
        this.actionToInvokeWhenEventArrives?.Invoke();
    }
}