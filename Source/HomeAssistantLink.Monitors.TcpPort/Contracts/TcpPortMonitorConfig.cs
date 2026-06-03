namespace HomeAssistantLink.Monitors.TcpPort.Contracts;

public class TcpPortMonitorConfig
{
    public IList<TcpPortTargetConfig> Targets { get; set; } = [];
}
