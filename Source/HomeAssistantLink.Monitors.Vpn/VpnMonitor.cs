namespace HomeAssistantLink.Monitors.Vpn;

using System.Net.NetworkInformation;
using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.Vpn.Contracts;
using Microsoft.Extensions.Options;

public class VpnMonitor(INetworkAdapterChangeMonitor monitor, IOptions<VpnMonitorConfig> options)
    : IMonitorBool
{
    private readonly object lockObject = new();
    private readonly INetworkAdapterChangeMonitor monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly VpnMonitorConfig options = options == null ? throw new ArgumentNullException(nameof(options)) : options.Value ?? throw new ArgumentNullException(nameof(options));
    private bool isVpnUp;
    private Action? actionToInvoke;

    public string EntityId => this.options.EntityId;

    public string NetworkInterfaceDescription => this.options.NetworkInterfaceDescription;

    public bool Value => this.isVpnUp;

    public void Start(Action action)
    {
        this.actionToInvoke = action;
        this.actionToInvoke?.Invoke();

        this.monitor.Start(this.NetworkAddressChanged);
        this.CheckNetworkAdapters();
    }

    public void Stop()
    {
        this.monitor.Stop();
        this.actionToInvoke = null;
    }

    private void NetworkAddressChanged()
    {
        this.CheckNetworkAdapters();
    }

    private void CheckNetworkAdapters()
    {
        lock (this.lockObject)
        {
            var wasVpnUp = this.isVpnUp;
            this.isVpnUp = NetworkInterface.GetAllNetworkInterfaces().Any(x => x.Description == this.NetworkInterfaceDescription);

            if (wasVpnUp != this.isVpnUp)
            {
                this.actionToInvoke?.Invoke();
            }
        }
    }
}