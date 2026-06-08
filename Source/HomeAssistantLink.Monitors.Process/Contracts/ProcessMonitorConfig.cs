namespace HomeAssistantLink.Monitors.Process.Contracts;

public sealed class ProcessMonitorConfig
{
    public IList<ProcessMonitorTargetConfig> Targets { get; set; } = [];
}
