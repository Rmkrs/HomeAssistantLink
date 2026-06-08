namespace HomeAssistantLink.Monitors.TcpPort;

using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Sockets;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.TcpPort.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class TcpPortMonitor(
    IOptions<TcpPortMonitorConfig> options,
    ILogger<TcpPortMonitor> logger) : IMonitor
{
    private static readonly TimeSpan stateRepublishInterval = TimeSpan.FromMinutes(5);

    private static readonly Action<ILogger, string, string, int, Exception?> probeFailed =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Debug,
            new EventId(1, nameof(probeFailed)),
            "TCP probe failed for monitor {MonitorName} target {Host}:{Port}.");

    private static readonly Action<ILogger, string, string, Exception?> publishFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2, nameof(publishFailed)),
            "Failed to publish {MonitorName} update for entity {EntityId}.");

    private readonly TcpPortMonitorConfig options = options == null
        ? throw new ArgumentNullException(nameof(options))
        : options.Value ?? throw new ArgumentNullException(nameof(options));

    private readonly ILogger<TcpPortMonitor> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ConcurrentDictionary<string, PublishedState> publishedStates =
        new(StringComparer.OrdinalIgnoreCase);

    private Func<EntityStateUpdate, CancellationToken, Task>? publishFunction;
    private CancellationTokenSource? cancellationTokenSource;
    private Task[] monitorTasks = [];

    public string Name => "TcpPort";

    public Task StartAsync(Func<EntityStateUpdate, CancellationToken, Task> publish, CancellationToken ct)
    {
        this.ValidateOptions();

        this.publishFunction = publish ?? throw new ArgumentNullException(nameof(publish));
        this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);

        this.monitorTasks =
        [
            .. this.options.Targets.Select(target =>
                this.MonitorTargetAsync(target, this.cancellationTokenSource.Token)),
        ];

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        var source = this.cancellationTokenSource;

        if (source != null)
        {
            await source.CancelAsync().ConfigureAwait(false);
        }

        try
        {
            await Task.WhenAll(this.monitorTasks).WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }

        source?.Dispose();

        this.cancellationTokenSource = null;
        this.monitorTasks = [];
        this.publishFunction = null;
        this.publishedStates.Clear();
    }

    private async Task MonitorTargetAsync(TcpPortTargetConfig target, CancellationToken cancellationToken)
    {
        await this.CheckAndPublishAsync(target, forcePublish: true, cancellationToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(target.ScanIntervalSeconds));

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await this.CheckAndPublishAsync(target, forcePublish: false, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CheckAndPublishAsync(
        TcpPortTargetConfig target,
        bool forcePublish,
        CancellationToken cancellationToken)
    {
        var isAvailable = await this.CheckPortAsync(target, cancellationToken).ConfigureAwait(false);

        var publishedState = this.publishedStates.GetOrAdd(
            target.EntityId,
            _ => new PublishedState());

        var shouldPublish =
            forcePublish ||
            publishedState.LastPublishedValue != isAvailable ||
            this.ShouldRepublish(publishedState);

        if (!shouldPublish)
        {
            return;
        }

        var update = new EntityStateUpdate(
            target.EntityId,
            HomeAssistantEntityType.Boolean,
            isAvailable);

        var published = await this.PublishAsync(update, cancellationToken).ConfigureAwait(false);

        if (published)
        {
            publishedState.LastPublishedValue = isAvailable;
            publishedState.LastPublishedAt = DateTimeOffset.UtcNow;
        }
    }

    private bool ShouldRepublish(PublishedState publishedState)
    {
        if (publishedState.LastPublishedValue == null)
        {
            return true;
        }

        return DateTimeOffset.UtcNow - publishedState.LastPublishedAt >= stateRepublishInterval;
    }

    private async Task<bool> CheckPortAsync(TcpPortTargetConfig target, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutSource = new CancellationTokenSource(target.TimeoutMilliseconds);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);

            using var client = new TcpClient();

            await client.ConnectAsync(target.Host, target.Port, linkedSource.Token).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            probeFailed(
                this.logger,
                this.GetTargetName(target),
                target.Host,
                target.Port,
                exception);

            return false;
        }
    }

    private async Task<bool> PublishAsync(EntityStateUpdate update, CancellationToken cancellationToken)
    {
        var callback = this.publishFunction;

        if (callback == null)
        {
            return false;
        }

        try
        {
            await callback(update, cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
            return false;
        }
        catch (Exception exception)
        {
            publishFailed(
                this.logger,
                this.Name,
                update.EntityId,
                exception);

            return false;
        }
    }

    private void ValidateOptions()
    {
        if (this.options.Targets.Count == 0)
        {
            throw new InvalidOperationException("At least one TCP port monitor target is required.");
        }

        foreach (var target in this.options.Targets)
        {
            this.ValidateTarget(target);
        }
    }

    private void ValidateTarget(TcpPortTargetConfig target)
    {
        if (string.IsNullOrWhiteSpace(target.EntityId))
        {
            throw new InvalidOperationException("TCP port monitor target entity id is required.");
        }

        if (string.IsNullOrWhiteSpace(target.Host))
        {
            throw new InvalidOperationException($"TCP port monitor target '{this.GetTargetName(target)}' host is required.");
        }

        if (target.Port is <= 0 or > 65535)
        {
            throw new InvalidOperationException($"TCP port monitor target '{this.GetTargetName(target)}' port must be between 1 and 65535.");
        }

        if (target.ScanIntervalSeconds <= 0)
        {
            throw new InvalidOperationException($"TCP port monitor target '{this.GetTargetName(target)}' scan interval must be greater than zero.");
        }

        if (target.TimeoutMilliseconds <= 0)
        {
            throw new InvalidOperationException($"TCP port monitor target '{this.GetTargetName(target)}' timeout must be greater than zero.");
        }
    }

    private string GetTargetName(TcpPortTargetConfig target)
    {
        return string.IsNullOrWhiteSpace(target.Name)
            ? string.Create(CultureInfo.InvariantCulture, $"{target.Host}:{target.Port}")
            : target.Name;
    }

    private sealed class PublishedState
    {
        public bool? LastPublishedValue { get; set; }

        public DateTimeOffset LastPublishedAt { get; set; }
    }
}
