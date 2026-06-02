namespace HomeAssistantLink.Monitors.Vpn;

using System.Net.NetworkInformation;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.Vpn.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class VpnMonitor(
    INetworkAdapterChangeMonitor monitor,
    IOptions<VpnMonitorConfig> options,
    ILogger<VpnMonitor> logger) : IMonitor
{
    private static readonly Action<ILogger, string, string, Exception?> publishFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(publishFailed)),
            "Failed to publish {MonitorName} update for entity {EntityId}.");

    private readonly Lock lockObject = new();
    private readonly INetworkAdapterChangeMonitor monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly ILogger<VpnMonitor> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly VpnMonitorConfig options = options == null
        ? throw new ArgumentNullException(nameof(options))
        : options.Value ?? throw new ArgumentNullException(nameof(options));

    private bool isVpnUp;
    private Func<EntityStateUpdate, CancellationToken, Task>? publish;
    private CancellationToken cancellationToken;

    public string Name => "VPN";

    public string EntityId => this.options.EntityId;

    public string NetworkInterfaceDescription => this.options.NetworkInterfaceDescription;

    public Task StartAsync(Func<EntityStateUpdate, CancellationToken, Task> publish, CancellationToken cancellationToken)
    {
        this.publish = publish ?? throw new ArgumentNullException(nameof(publish));
        this.cancellationToken = cancellationToken;

        this.monitor.Start(this.NetworkAddressChanged);
        this.CheckNetworkAdapters(forcePublish: true);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.monitor.StopMonitoring();
        this.publish = null;

        return Task.CompletedTask;
    }

    private void NetworkAddressChanged()
    {
        this.CheckNetworkAdapters(forcePublish: false);
    }

    private void CheckNetworkAdapters(bool forcePublish)
    {
        EntityStateUpdate? update = null;

        lock (this.lockObject)
        {
            var wasVpnUp = this.isVpnUp;
            this.isVpnUp = NetworkInterface
                .GetAllNetworkInterfaces()
                .Any(x => string.Equals(
                    x.Description,
                    this.NetworkInterfaceDescription,
                    StringComparison.Ordinal));

            if (forcePublish || wasVpnUp != this.isVpnUp)
            {
                update = new EntityStateUpdate(
                    this.EntityId,
                    HomeAssistantEntityType.Boolean,
                    this.isVpnUp);
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