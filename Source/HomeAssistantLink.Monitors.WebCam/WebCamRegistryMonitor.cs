namespace HomeAssistantLink.Monitors.WebCam;

using System.Management;
using System.Security.Principal;

using HomeAssistantLink.Monitors.WebCam.Contracts;

public sealed class WebCamRegistryMonitor : IWebCamRegistryMonitor, IDisposable
{
    private readonly ManagementEventWatcher watcher = new();
    private Action? actionToInvokeWhenEventArrives;
    private bool disposed;

    public WebCamRegistryMonitor()
    {
        var currentUserGuid = WindowsIdentity.GetCurrent().User?.Value;
        var userQuery = new WqlEventQuery(
            @$"SELECT * FROM RegistryTreeChangeEvent WHERE Hive = 'HKEY_USERS' AND RootPath = '{currentUserGuid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam'");

        this.watcher.Query = userQuery;
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