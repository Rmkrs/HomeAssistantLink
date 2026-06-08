namespace HomeAssistantLink.Host.TrayApp;

using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

public sealed partial class DisplaySnapshotPublisher(
    IUserSessionEventClient userSessionEventClient,
    ILogger<DisplaySnapshotPublisher> logger) : BackgroundService
{
    private static readonly TimeSpan snapshotInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SystemEvents.DisplaySettingsChanged += this.DisplaySettingsChanged;

        try
        {
            this.PublishSnapshot();

            using var timer = new PeriodicTimer(snapshotInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                this.PublishSnapshot();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        finally
        {
            SystemEvents.DisplaySettingsChanged -= this.DisplaySettingsChanged;
        }
    }

    private void DisplaySettingsChanged(object? sender, EventArgs e)
    {
        this.PublishSnapshot();
    }

    private void PublishSnapshot()
    {
        try
        {
            var displays = Screen.AllScreens
                .Select(screen => new DisplayObservationModel
                {
                    DeviceName = screen.DeviceName,
                    IsConnected = true,
                })
                .ToList();

            DisplaySnapshotCollected(logger, displays.Count);

            userSessionEventClient.PublishDisplaySnapshot(displays);
        }
        catch (Exception exception)
        {
            DisplaySnapshotFailed(logger, exception);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Display snapshot collected. Count: {DisplayCount}")]
    private static partial void DisplaySnapshotCollected(
        ILogger logger,
        int displayCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to publish display snapshot.")]
    private static partial void DisplaySnapshotFailed(
        ILogger logger,
        Exception exception);
}
