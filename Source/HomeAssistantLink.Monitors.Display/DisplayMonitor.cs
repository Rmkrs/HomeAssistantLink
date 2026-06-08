namespace HomeAssistantLink.Monitors.Display;

using System.Collections.Concurrent;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.Display.Contracts;
using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class DisplayMonitor(
    IOptions<DisplayMonitorConfig> options,
    ILogger<DisplayMonitor> logger) : IMonitor, IUserSessionEventSink
{
    private static readonly TimeSpan stateRepublishInterval = TimeSpan.FromMinutes(5);

    private readonly DisplayMonitorConfig options = options.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<DisplayMonitor> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ConcurrentDictionary<string, PublishedState> publishedStates =
        new(StringComparer.OrdinalIgnoreCase);

    private Func<EntityStateUpdate, CancellationToken, Task>? publishFunction;
    private CancellationToken cancellationToken;

    public string Name => "DisplayMonitor";

    public Task StartAsync(
        Func<EntityStateUpdate, CancellationToken, Task> publish,
        CancellationToken ct)
    {
        this.ValidateOptions();

        this.publishFunction = publish ?? throw new ArgumentNullException(nameof(publish));
        this.cancellationToken = ct;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        this.publishFunction = null;
        this.publishedStates.Clear();

        return Task.CompletedTask;
    }

    public async Task HandleAsync(
        UserSessionEventRequestModel request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.EventType != UserSessionEventType.DisplaySnapshot)
        {
            return;
        }

        foreach (var target in this.options.Targets)
        {
            var observation = request.Displays.FirstOrDefault(display =>
                string.Equals(display.DeviceName, target.DeviceName, StringComparison.OrdinalIgnoreCase));

            var isConnected = observation?.IsConnected == true;

            await this.CheckAndPublishAsync(
                target,
                isConnected,
                forcePublish: false,
                ct).ConfigureAwait(false);
        }
    }

    private async Task CheckAndPublishAsync(
        DisplayMonitorTargetConfig target,
        bool isConnected,
        bool forcePublish,
        CancellationToken ct)
    {
        var publishedState = this.publishedStates.GetOrAdd(
            target.EntityId,
            _ => new PublishedState());

        var shouldPublish =
            forcePublish ||
            publishedState.LastPublishedValue != isConnected ||
            this.ShouldRepublish(publishedState);

        if (!shouldPublish)
        {
            return;
        }

        DisplayStateChanged(
            this.logger,
            target.Name,
            target.DeviceName,
            isConnected);

        var update = new EntityStateUpdate(
            target.EntityId,
            HomeAssistantEntityType.Boolean,
            isConnected);

        var published = await this.PublishAsync(update, ct).ConfigureAwait(false);

        if (published)
        {
            publishedState.LastPublishedValue = isConnected;
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

    private async Task<bool> PublishAsync(
        EntityStateUpdate update,
        CancellationToken ct)
    {
        var callback = this.publishFunction;

        if (callback == null)
        {
            PublishSkippedBecauseMonitorIsNotStarted(
                this.logger,
                update.EntityId);

            return false;
        }

        try
        {
            await callback(update, ct).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException) when (this.cancellationToken.IsCancellationRequested || ct.IsCancellationRequested)
        {
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
            throw new InvalidOperationException("At least one display monitor target is required.");
        }

        foreach (var target in this.options.Targets)
        {
            ValidateTarget(target);
        }
    }

    private static void ValidateTarget(DisplayMonitorTargetConfig target)
    {
        if (string.IsNullOrWhiteSpace(target.Name))
        {
            throw new InvalidOperationException("Display monitor target name is required.");
        }

        if (string.IsNullOrWhiteSpace(target.EntityId))
        {
            throw new InvalidOperationException($"Display monitor target '{target.Name}' entity id is required.");
        }

        if (string.IsNullOrWhiteSpace(target.DeviceName))
        {
            throw new InvalidOperationException($"Display monitor target '{target.Name}' device name is required.");
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Display state changed. TargetName: {TargetName}, DeviceName: {DeviceName}, IsConnected: {IsConnected}")]
    private static partial void DisplayStateChanged(
        ILogger logger,
        string targetName,
        string deviceName,
        bool isConnected);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to publish {MonitorName} update for entity {EntityId}.")]
    private static partial void PublishFailed(
        ILogger logger,
        string monitorName,
        string entityId,
        Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Display update skipped because monitor is not started. EntityId: {EntityId}")]
    private static partial void PublishSkippedBecauseMonitorIsNotStarted(
        ILogger logger,
        string entityId);

    private sealed class PublishedState
    {
        public bool? LastPublishedValue { get; set; }

        public DateTimeOffset LastPublishedAt { get; set; }
    }
}
