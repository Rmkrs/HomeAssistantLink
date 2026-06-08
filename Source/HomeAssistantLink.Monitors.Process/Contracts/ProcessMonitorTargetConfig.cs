namespace HomeAssistantLink.Monitors.Process.Contracts;

public sealed class ProcessMonitorTargetConfig
{
    public string Name { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string ProcessName { get; set; } = string.Empty;

    public int ScanIntervalSeconds { get; set; } = 30;
}
