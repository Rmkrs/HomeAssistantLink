namespace HomeAssistantLink.Monitors.Vpn.Contracts;

public interface INetworkAdapterChangeMonitor
{
    void Start(Action action);

    void StopMonitoring();
}