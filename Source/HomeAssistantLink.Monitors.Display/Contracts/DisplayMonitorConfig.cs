namespace HomeAssistantLink.Monitors.Display.Contracts;

public class DisplayMonitorConfig
{
    public IList<DisplayMonitorTargetConfig> Targets { get; set; } = [];
}
