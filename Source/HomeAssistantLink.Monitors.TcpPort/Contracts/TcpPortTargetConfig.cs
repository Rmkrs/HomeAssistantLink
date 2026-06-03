namespace HomeAssistantLink.Monitors.TcpPort.Contracts;

public class TcpPortTargetConfig
{
    public string Name { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public int ScanIntervalSeconds { get; set; } = 30;

    public int TimeoutMilliseconds { get; set; } = 2000;
}
