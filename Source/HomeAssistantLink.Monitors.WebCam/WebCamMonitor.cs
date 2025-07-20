namespace HomeAssistantLink.Monitors.WebCam;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.WebCam.Contracts;
using Microsoft.Extensions.Options;

public class WebCamMonitor(IWebCamIterator iterator, IWebCamRegistryMonitor monitor, IOptions<WebCamMonitorConfig> options)
    : IMonitorBool
{
    private readonly object lockObject = new();
    private readonly WebCamMonitorConfig options = options == null ? throw new ArgumentNullException(nameof(options)) : options.Value ?? throw new ArgumentNullException(nameof(options));
    private Action? actionToInvoke;
    private bool isInUse;

    public string EntityId => this.options.EntityId;

    public bool Value => this.isInUse;

    public void Start(Action action)
    {
        this.actionToInvoke = action;
        this.actionToInvoke?.Invoke();
        monitor.Start(this.RegistryChanged);
        this.RegistryChanged();
    }

    public void Stop()
    {
        monitor.Stop();
    }

    private void RegistryChanged()
    {
        lock (this.lockObject)
        {
            var wasInUse = this.isInUse;
            this.isInUse = iterator.Iterate().Any(x => x.IsInUse);

            if (wasInUse != this.isInUse)
            {
                this.actionToInvoke?.Invoke();
            }
        }
    }
}