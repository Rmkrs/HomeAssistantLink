namespace HomeAssistantLink.Monitors.Vpn;

using System.Management;
using HomeAssistantLink.Monitors.Vpn.Contracts;

public class NetworkAdapterChangeMonitor : INetworkAdapterChangeMonitor
{
    private readonly ManagementEventWatcher watcher = new();
    private Action? actionToInvokeWhenEventArrives;

    public NetworkAdapterChangeMonitor()
    {
        var networkMonitorQuery = new EventQuery("Select * From __InstanceModificationEvent Within 1 where TargetInstance ISA 'Win32_NetworkAdapter'");
        this.watcher.Query = networkMonitorQuery;
        this.watcher.EventArrived += this.EventArrived;
    }

    public void Start(Action action)
    {
        this.actionToInvokeWhenEventArrives = action;
        this.watcher.Start();
    }

    public void Stop()
    {
        this.watcher.Stop();
        this.actionToInvokeWhenEventArrives = null;
    }

    private void EventArrived(object sender, EventArrivedEventArgs e)
    {
        this.actionToInvokeWhenEventArrives?.Invoke();
    }
}