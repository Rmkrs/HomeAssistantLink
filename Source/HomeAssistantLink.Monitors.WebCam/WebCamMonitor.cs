namespace HomeAssistantLink.Monitors.WebCam;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.WebCam.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class WebCamMonitor(
    IWebCamIterator iterator,
    IWebCamRegistryMonitor monitor,
    IOptions<WebCamMonitorConfig> options,
    ILogger<WebCamMonitor> logger) : IMonitor
{
    private static readonly Action<ILogger, string, string, Exception?> publishFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(publishFailed)),
            "Failed to publish {MonitorName} update for entity {EntityId}.");

    private readonly Lock lockObject = new();
    private readonly IWebCamIterator iterator = iterator ?? throw new ArgumentNullException(nameof(iterator));
    private readonly IWebCamRegistryMonitor monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly ILogger<WebCamMonitor> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly WebCamMonitorConfig options = options == null
        ? throw new ArgumentNullException(nameof(options))
        : options.Value ?? throw new ArgumentNullException(nameof(options));

    private Func<EntityStateUpdate, CancellationToken, Task>? publish;
    private CancellationToken cancellationToken;
    private bool isInUse;

    public string Name => "WebCam";

    public string EntityId => this.options.EntityId;

    public Task StartAsync(Func<EntityStateUpdate, CancellationToken, Task> publish, CancellationToken ct)
    {
        this.publish = publish ?? throw new ArgumentNullException(nameof(publish));
        this.cancellationToken = ct;

        this.monitor.Start(this.RegistryChanged);
        this.CheckRegistry(forcePublish: true);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        this.monitor.StopMonitoring();
        this.publish = null;

        return Task.CompletedTask;
    }

    private void RegistryChanged()
    {
        this.CheckRegistry(forcePublish: false);
    }

    private void CheckRegistry(bool forcePublish)
    {
        EntityStateUpdate? update = null;

        lock (this.lockObject)
        {
            var wasInUse = this.isInUse;
            this.isInUse = this.iterator.Iterate().Any(x => x.IsInUse);

            if (forcePublish || wasInUse != this.isInUse)
            {
                update = new EntityStateUpdate(
                    this.EntityId,
                    HomeAssistantEntityType.Boolean,
                    this.isInUse);
            }
        }

        if (update != null)
        {
            _ = this.PublishAsync(update);
        }
    }

    private async Task PublishAsync(EntityStateUpdate update)
    {
        var callback = this.publish;

        if (callback == null)
        {
            return;
        }

        try
        {
            await callback(update, this.cancellationToken);
        }
        catch (OperationCanceledException) when (this.cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception exception)
        {
            publishFailed(
                this.logger,
                this.Name,
                update.EntityId,
                exception);
        }
    }
}