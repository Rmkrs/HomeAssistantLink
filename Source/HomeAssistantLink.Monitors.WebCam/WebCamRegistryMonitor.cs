namespace HomeAssistantLink.Monitors.WebCam;

using System.Management;
using System.Security.Principal;

using HomeAssistantLink.Monitors.WebCam.Contracts;

public class WebCamRegistryMonitor : IWebCamRegistryMonitor
{
    private readonly ManagementEventWatcher watcher = new();
    private Action? actionToInvokeWhenEventArrives;

    public WebCamRegistryMonitor()
    {
        var currentUserGuid = WindowsIdentity.GetCurrent().User?.Value;
        var userQuery = new WqlEventQuery(@$"SELECT * FROM RegistryTreeChangeEvent WHERE Hive = 'HKEY_USERS' AND RootPath = '{currentUserGuid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam'");

        this.watcher.Query = userQuery;
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