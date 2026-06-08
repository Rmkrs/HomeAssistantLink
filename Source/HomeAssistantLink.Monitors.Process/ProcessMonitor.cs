namespace HomeAssistantLink.Monitors.Process;

using System.Collections.Concurrent;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.Process.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class ProcessMonitor(
    IOptions<ProcessMonitorConfig> options,
    ILogger<ProcessMonitor> logger) : IMonitor
{
    private static readonly TimeSpan stateRepublishInterval = TimeSpan.FromMinutes(5);

    private readonly ProcessMonitorConfig options = options.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<ProcessMonitor> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ConcurrentDictionary<string, PublishedState> publishedStates =
        new(StringComparer.OrdinalIgnoreCase);

    private Func<EntityStateUpdate, CancellationToken, Task>? publishFunction;
    private CancellationTokenSource? cancellationTokenSource;
    private Task[] monitorTasks = [];

    public string Name => "ProcessMonitor";

    public Task StartAsync(
        Func<EntityStateUpdate, CancellationToken, Task> publish,
        CancellationToken ct)
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

    private async Task MonitorTargetAsync(
        ProcessMonitorTargetConfig target,
        CancellationToken cancellationToken)
    {
        await this.CheckAndPublishAsync(
            target,
            forcePublish: true,
            cancellationToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(target.ScanIntervalSeconds));

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await this.CheckAndPublishAsync(
                target,
                forcePublish: false,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CheckAndPublishAsync(
        ProcessMonitorTargetConfig target,
        bool forcePublish,
        CancellationToken cancellationToken)
    {
        bool isRunning;

        try
        {
            isRunning = IsProcessRunning(target.ProcessName);
        }
        catch (Exception exception)
        {
            var targetName = this.GetTargetName(target);

            ProcessProbeFailed(
                this.logger,
                targetName,
                target.ProcessName,
                exception);

            return;
        }

        var publishedState = this.publishedStates.GetOrAdd(
            target.EntityId,
            _ => new PublishedState());

        var shouldPublish =
            forcePublish ||
            publishedState.LastPublishedValue != isRunning ||
            this.ShouldRepublish(publishedState);

        if (!shouldPublish)
        {
            return;
        }

        var update = new EntityStateUpdate(
            target.EntityId,
            HomeAssistantEntityType.Boolean,
            isRunning);

        var published = await this.PublishAsync(update, cancellationToken).ConfigureAwait(false);

        if (published)
        {
            publishedState.LastPublishedValue = isRunning;
            publishedState.LastPublishedAt = DateTimeOffset.UtcNow;
        }
    }

    private static bool IsProcessRunning(string processName)
    {
        var normalizedProcessName = Path.GetFileNameWithoutExtension(processName);

        var processes = System.Diagnostics.Process.GetProcessesByName(normalizedProcessName);

        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
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

    private async Task<bool> PublishAsync(
        EntityStateUpdate update,
        CancellationToken cancellationToken)
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
            PublishFailed(
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
            throw new InvalidOperationException("At least one process monitor target is required.");
        }

        foreach (var target in this.options.Targets)
        {
            this.ValidateTarget(target);
        }
    }

    private void ValidateTarget(ProcessMonitorTargetConfig target)
    {
        if (string.IsNullOrWhiteSpace(target.EntityId))
        {
            throw new InvalidOperationException("Process monitor target entity id is required.");
        }

        if (string.IsNullOrWhiteSpace(target.ProcessName))
        {
            throw new InvalidOperationException($"Process monitor target '{this.GetTargetName(target)}' process name is required.");
        }

        if (target.ScanIntervalSeconds <= 0)
        {
            throw new InvalidOperationException($"Process monitor target '{this.GetTargetName(target)}' scan interval must be greater than zero.");
        }
    }

    private string GetTargetName(ProcessMonitorTargetConfig target)
    {
        return string.IsNullOrWhiteSpace(target.Name)
            ? target.ProcessName
            : target.Name;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Process probe failed for monitor target {TargetName} process {ProcessName}.")]
    private static partial void ProcessProbeFailed(
        ILogger logger,
        string targetName,
        string processName,
        Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to publish {MonitorName} update for entity {EntityId}.")]
    private static partial void PublishFailed(
        ILogger logger,
        string monitorName,
        string entityId,
        Exception exception);

    private sealed class PublishedState
    {
        public bool? LastPublishedValue { get; set; }

        public DateTimeOffset LastPublishedAt { get; set; }
    }
}
