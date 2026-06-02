namespace HomeAssistantLink.Monitors.WebCam.Contracts;

public interface IWebCamRegistryMonitor
{
    void Start(Action action);

    void StopMonitoring();
}